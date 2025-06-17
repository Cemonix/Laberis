using server.Models.Domain;
using server.Models.DTOs.Asset;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Models.Domain.Enums;
using server.Utils;
using server.Exceptions;

namespace server.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AssetService> _logger;
    private const string DefaultBucketName = "laberis-assets";

    public AssetService(
        IAssetRepository assetRepository,
        IFileStorageService fileStorageService,
        ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<AssetDto>> GetAssetsForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching assets for project: {ProjectId}", projectId);

        var (assets, totalCount) = await _assetRepository.GetAllWithCountAsync(
            filter: a => a.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} assets for project: {ProjectId}", assets.Count(), projectId);

        var assetDtos = assets.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AssetDto>
        {
            Data = assetDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<AssetDto?> GetAssetByIdAsync(int assetId)
    {
        _logger.LogInformation("Fetching asset with ID: {AssetId}", assetId);
        
        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found.", assetId);
            return null;
        }

        return MapToDto(asset);
    }

    public async Task<AssetDto> CreateAssetAsync(int projectId, CreateAssetDto createDto)
    {
        _logger.LogInformation("Creating new asset for project: {ProjectId}", projectId);

        var asset = new Asset
        {
            ExternalId = createDto.ExternalId,
            Filename = createDto.Filename,
            MimeType = createDto.MimeType,
            SizeBytes = createDto.SizeBytes,
            Width = createDto.Width,
            Height = createDto.Height,
            DurationMs = createDto.DurationMs,
            Metadata = createDto.Metadata,
            ProjectId = projectId,
            DataSourceId = createDto.DataSourceId,
            Status = AssetStatus.PENDING_IMPORT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _assetRepository.AddAsync(asset);
        await _assetRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created asset with ID: {AssetId}", asset.AssetId);
        
        return MapToDto(asset);
    }

    public async Task<AssetDto?> UpdateAssetAsync(int assetId, UpdateAssetDto updateDto)
    {
        _logger.LogInformation("Updating asset with ID: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found for update.", assetId);
            return null;
        }

        var updatedAsset = asset with
        {
            Filename = updateDto.Filename ?? asset.Filename,
            MimeType = updateDto.MimeType ?? asset.MimeType,
            SizeBytes = updateDto.SizeBytes ?? asset.SizeBytes,
            Width = updateDto.Width ?? asset.Width,
            Height = updateDto.Height ?? asset.Height,
            DurationMs = updateDto.DurationMs ?? asset.DurationMs,
            Metadata = updateDto.Metadata ?? asset.Metadata,
            Status = updateDto.Status,
            DataSourceId = updateDto.DataSourceId ?? asset.DataSourceId,
            UpdatedAt = DateTime.UtcNow
        };

        _assetRepository.Update(updatedAsset);
        await _assetRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated asset with ID: {AssetId}", assetId);
        
        return MapToDto(updatedAsset);
    }

    public async Task<bool> DeleteAssetAsync(int assetId)
    {
        _logger.LogInformation("Deleting asset with ID: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found for deletion.", assetId);
            return false;
        }

        _assetRepository.Remove(asset);
        await _assetRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted asset with ID: {AssetId}", assetId);
        
        return true;
    }

    public async Task<UploadResultDto> UploadAssetAsync(int projectId, UploadAssetDto uploadDto)
    {
        _logger.LogInformation("Starting upload for file '{FileName}' in project {ProjectId}", uploadDto.File.FileName, projectId);

        try
        {
            // Validate file
            var validationError = ValidateFile(uploadDto.File);
            if (validationError != null)
            {
                return CreateFailedUploadResult(uploadDto.File.FileName, validationError, ErrorTypes.ValidationError.ToStringValue());
            }

            // Generate unique file name and storage path
            var sanitizedFileName = FileProcessingUtils.SanitizeFileName(uploadDto.File.FileName);
            var uniqueObjectName = FileProcessingUtils.GenerateUniqueFileName(sanitizedFileName, projectId);
            var mimeType = FileProcessingUtils.GetMimeType(uploadDto.File.FileName);

            // Extract image dimensions and validate if it's an image
            (int Width, int Height)? dimensions = null;
            using (var stream = uploadDto.File.OpenReadStream())
            {
                if (mimeType.StartsWith("image/"))
                {
                    // Use ImageSharp for robust image validation and dimension extraction
                    if (!FileProcessingUtils.IsValidImageStream(stream))
                    {
                        return CreateFailedUploadResult(uploadDto.File.FileName, "File appears to be corrupted or is not a valid image", ErrorTypes.ValidationError.ToStringValue());
                    }
                    
                    dimensions = FileProcessingUtils.GetImageDimensions(stream);
                }
            }

            // Upload file to storage
            string storagePath;
            using (var stream = uploadDto.File.OpenReadStream())
            {
                storagePath = await _fileStorageService.UploadFileAsync(stream, DefaultBucketName, uniqueObjectName);
            }

            // Create asset record in database
            var asset = new Asset
            {
                ExternalId = storagePath,
                Filename = sanitizedFileName,
                MimeType = mimeType,
                SizeBytes = uploadDto.File.Length,
                Width = dimensions?.Width,
                Height = dimensions?.Height,
                Metadata = uploadDto.Metadata,
                ProjectId = projectId,
                DataSourceId = uploadDto.DataSourceId,
                Status = AssetStatus.IMPORTED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _assetRepository.AddAsync(asset);
            await _assetRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded asset with ID: {AssetId} for file '{FileName}'", asset.AssetId, uploadDto.File.FileName);

            return new UploadResultDto
            {
                Asset = MapToDto(asset),
                Filename = uploadDto.File.FileName,
                Success = true
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorTypes.ValidationError.ToStringValue());
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorTypes.StorageError.ToStringValue());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, "An unexpected error occurred during upload", ErrorTypes.InternalServerError.ToStringValue());
        }
    }

    public async Task<BulkUploadResultDto> UploadAssetsAsync(int projectId, BulkUploadAssetDto bulkUploadDto)
    {
        _logger.LogInformation("Starting bulk upload of {FileCount} files for project {ProjectId}", bulkUploadDto.Files.Count, projectId);

        var results = new List<UploadResultDto>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var file in bulkUploadDto.Files)
        {
            var uploadDto = new UploadAssetDto
            {
                File = file,
                DataSourceId = bulkUploadDto.DataSourceId,
                Metadata = bulkUploadDto.Metadata
            };

            var result = await UploadAssetAsync(projectId, uploadDto);
            results.Add(result);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        var summary = $"Uploaded {successCount} of {bulkUploadDto.Files.Count} files successfully";
        if (failureCount > 0)
        {
            summary += $", {failureCount} failed";
        }

        _logger.LogInformation("Bulk upload completed for project {ProjectId}: {Summary}", projectId, summary);

        return new BulkUploadResultDto
        {
            Results = results,
            TotalFiles = bulkUploadDto.Files.Count,
            SuccessfulUploads = successCount,
            FailedUploads = failureCount,
            Summary = summary
        };
    }

    private static string? ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "File is empty";
        }

        if (file.Length > FileProcessingUtils.MaxFileSizeBytes)
        {
            return $"File size exceeds maximum allowed size of {FileProcessingUtils.MaxFileSizeBytes / (1024 * 1024)}MB";
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return "File name is required";
        }

        if (!FileProcessingUtils.IsValidImageFile(file.FileName, file.ContentType))
        {
            return "File type is not supported. Only image files are allowed.";
        }

        // Additional validation using ImageSharp to ensure the file is actually a valid image
        try
        {
            using var stream = file.OpenReadStream();
            if (!FileProcessingUtils.IsValidImageStream(stream))
            {
                return "File is not a valid image or is corrupted.";
            }
        }
        catch (Exception)
        {
            return "Unable to process the image file. It may be corrupted.";
        }

        return null;
    }

    private static UploadResultDto CreateFailedUploadResult(string filename, string errorMessage, string errorType)
    {
        return new UploadResultDto
        {
            Filename = filename,
            Success = false,
            ErrorMessage = errorMessage,
            ErrorType = errorType
        };
    }

    private static AssetDto MapToDto(Asset asset)
    {
        return new AssetDto
        {
            Id = asset.AssetId,
            ExternalId = asset.ExternalId,
            Filename = asset.Filename,
            MimeType = asset.MimeType,
            SizeBytes = asset.SizeBytes,
            Width = asset.Width,
            Height = asset.Height,
            DurationMs = asset.DurationMs,
            Status = asset.Status,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt,
            ProjectId = asset.ProjectId,
            DataSourceId = asset.DataSourceId
        };
    }
}
