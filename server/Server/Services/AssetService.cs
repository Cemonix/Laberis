using server.Models.Domain;
using server.Models.DTOs.Asset;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Models.Domain.Enums;
using server.Models.Internal;
using server.Utils;
using server.Exceptions;
using LaberisTask = server.Models.Domain.Task;

namespace server.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IStorageService _storageService;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IDomainEventService _domainEventService;
    private readonly ITaskService _taskService;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetRepository assetRepository,
        IFileStorageService fileStorageService,
        IDataSourceRepository dataSourceRepository,
        IStorageService storageService,
        IWorkflowStageRepository workflowStageRepository,
        IDomainEventService domainEventService,
        ITaskService taskService,
        ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _domainEventService = domainEventService ?? throw new ArgumentNullException(nameof(domainEventService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region GET Methods

    public async Task<PaginatedResponse<AssetDto>> GetAssetsForProjectAsync(
        int projectId,
        int? dataSourceId = null,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25,
        bool includeUrls = true, int urlExpiryInSeconds = 3600)
    {
        _logger.LogInformation("Fetching assets for project: {ProjectId}, dataSource: {DataSourceId}", projectId, dataSourceId);

        var (assets, totalCount) = await _assetRepository.GetAllWithCountAsync(
            filter: a => a.ProjectId == projectId && (dataSourceId == null || a.DataSourceId == dataSourceId),
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} assets for project: {ProjectId}", assets.Count(), projectId);

        var assetDtos = new List<AssetDto>();

        foreach (var asset in assets)
        {
            string? imageUrl = null;

            if (includeUrls)
            {
                try
                {
                    // Get the data source to determine the bucket name
                    var dataSource = await _dataSourceRepository.GetByIdAsync(asset.DataSourceId);
                    if (dataSource != null)
                    {
                        var bucketName = _storageService.GenerateBucketName(asset.ProjectId, dataSource.Name);
                        imageUrl = await _storageService.GetPresignedUrlAsync(bucketName, asset.ExternalId, urlExpiryInSeconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate URL for asset {AssetId}", asset.AssetId);
                    // Continue without URL rather than failing the entire request
                }
            }

            assetDtos.Add(new AssetDto
            {
                Id = asset.AssetId,
                Filename = asset.Filename,
                MimeType = asset.MimeType,
                SizeBytes = asset.SizeBytes,
                Width = asset.Width,
                Height = asset.Height,
                DurationMs = asset.DurationMs,
                Status = asset.Status,
                CreatedAt = asset.CreatedAt,
                UpdatedAt = asset.UpdatedAt,
                ImageUrl = imageUrl
            });
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AssetDto>
        {
            Data = assetDtos.ToArray(),
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

        // Generate presigned URL for the asset
        string? imageUrl = null;
        try
        {
            // Get the data source to determine the bucket name
            var dataSource = await _dataSourceRepository.GetByIdAsync(asset.DataSourceId);
            if (dataSource != null)
            {
                var bucketName = _storageService.GenerateBucketName(asset.ProjectId, dataSource.Name);
                imageUrl = await _storageService.GetPresignedUrlAsync(bucketName, asset.ExternalId, 3600); // 1 hour expiry
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate URL for asset {AssetId}", assetId);
            // Continue without URL rather than failing the entire request
        }

        return MapToDto(asset, imageUrl);
    }

    /// <summary>
    /// Gets the count of available assets for task creation in a specific data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>Count of available assets (without a task) in the specified data source.</returns>
    public async Task<int> GetAvailableAssetsCountAsync(int projectId)
    {
        _logger.LogInformation("Getting available assets count for project {ProjectId}", projectId);

        var count = await _assetRepository.GetAvailableAssetsCountAsync(projectId);

        _logger.LogInformation("Project {ProjectId} has {AssetsCount} available assets", projectId, count);
        return count;
    }

    /// <summary>
    /// Gets the count of available assets for task creation in a specific data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>Count of available assets (without a task) in the specified data source.</returns>
    public async Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId)
    {
        _logger.LogInformation("Getting available assets count for project {ProjectId} and data source {DataSourceId}", projectId, dataSourceId);

        var assets = await _assetRepository.GetAvailableAssetsFromDataSourceAsync(projectId, dataSourceId);

        _logger.LogInformation("Project {ProjectId} has {AssetsCount} available assets in data source {DataSourceId}", projectId, assets.Count(), dataSourceId);
        return assets;
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

    #endregion

    public async Task<AssetDto?> UpdateAssetAsync(int assetId, UpdateAssetDto updateDto)
    {
        _logger.LogInformation("Updating asset with ID: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId);

        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found for update.", assetId);
            return null;
        }

        asset.Filename = updateDto.Filename ?? asset.Filename;
        asset.MimeType = updateDto.MimeType ?? asset.MimeType;
        asset.SizeBytes = updateDto.SizeBytes ?? asset.SizeBytes;
        asset.Width = updateDto.Width ?? asset.Width;
        asset.Height = updateDto.Height ?? asset.Height;
        asset.DurationMs = updateDto.DurationMs ?? asset.DurationMs;
        asset.Metadata = updateDto.Metadata ?? asset.Metadata;
        // Auto-create tasks if asset was just marked as IMPORTED
        bool wasJustImported = updateDto.Status == AssetStatus.IMPORTED && asset.Status != AssetStatus.IMPORTED;

        asset.Status = updateDto.Status;
        asset.DataSourceId = updateDto.DataSourceId ?? asset.DataSourceId;
        asset.UpdatedAt = DateTime.UtcNow;

        await _assetRepository.SaveChangesAsync();

        if (wasJustImported)
        {
            await TryCreateTasksForNewAssetAsync(asset);
        }

        _logger.LogInformation("Successfully updated asset with ID: {AssetId}", assetId);

        return MapToDto(asset);
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

    /// <summary>
    /// Publishes domain event for a newly imported asset to trigger task creation
    /// </summary>
    /// <param name="asset">The asset that was just imported.</param>
    private async System.Threading.Tasks.Task TryCreateTasksForNewAssetAsync(Asset asset)
    {
        try
        {
            _logger.LogInformation("Publishing asset imported event for asset {AssetId} in data source {DataSourceId}",
                asset.AssetId, asset.DataSourceId);

            // Publish domain event - this will be handled by AssetImportedEventHandler
            // which will create the appropriate tasks
            var assetImportedEvent = new AssetImportedEvent(asset);
            await _domainEventService.PublishAsync(assetImportedEvent);

            _logger.LogInformation("Successfully published asset imported event for asset {AssetId}", asset.AssetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish asset imported event for asset {AssetId}", asset.AssetId);
            // Don't throw - asset import should succeed even if task creation fails
        }
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
                return CreateFailedUploadResult(uploadDto.File.FileName, validationError, ErrorType.ValidationError);
            }

            // Generate unique file name and storage path
            var sanitizedFileName = FileProcessingUtils.SanitizeFileName(uploadDto.File.FileName);
            var uniqueObjectName = FileProcessingUtils.GenerateUniqueFileName(sanitizedFileName);
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
                        return CreateFailedUploadResult(uploadDto.File.FileName, "File appears to be corrupted or is not a valid image", ErrorType.ValidationError);
                    }

                    dimensions = FileProcessingUtils.GetImageDimensions(stream);
                }
            }

            // Get data source to determine bucket name
            var dataSource = await _dataSourceRepository.GetByIdAsync(uploadDto.DataSourceId);
            if (dataSource == null)
            {
                return CreateFailedUploadResult(uploadDto.File.FileName, $"DataSource with ID {uploadDto.DataSourceId} not found", ErrorType.ValidationError);
            }

            string bucketName = _storageService.GenerateBucketName(projectId, dataSource.Name);

            // Upload file to storage using dynamic bucket naming
            string storagePath;
            using (var stream = uploadDto.File.OpenReadStream())
            {
                storagePath = await _fileStorageService.UploadFileAsync(stream, bucketName, uniqueObjectName);
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

            // Trigger automatic task creation for workflow stages that use this data source
            try
            {
                await TriggerAutomaticTaskCreationAsync(projectId, uploadDto.DataSourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger automatic task creation after asset upload to data source {DataSourceId} in project {ProjectId}",
                    uploadDto.DataSourceId, projectId);
                // Don't fail the upload because of task creation issues
            }

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
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorType.ValidationError);
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorType.StorageError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, "An unexpected error occurred during upload", ErrorType.InternalServerError);
        }
    }

    public async Task<BulkUploadResultDto> UploadAssetsAsync(int projectId, BulkUploadAssetDto bulkUploadDto)
    {
        _logger.LogInformation("Starting bulk upload of {FileCount} files for project {ProjectId}", bulkUploadDto.Files.Count, projectId);

        // Validate that files are provided
        if (bulkUploadDto.Files == null || bulkUploadDto.Files.Count == 0)
        {
            _logger.LogWarning("Bulk upload attempted with no files for project {ProjectId}", projectId);
            return new BulkUploadResultDto
            {
                Results = [],
                TotalFiles = 0,
                SuccessfulUploads = 0,
                FailedUploads = 0,
                Summary = "No files provided for upload"
            };
        }

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

        // Trigger automatic task creation for workflow stages that use this data source
        if (successCount > 0)
        {
            try
            {
                await TriggerAutomaticTaskCreationAsync(projectId, bulkUploadDto.DataSourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger automatic task creation after asset upload to data source {DataSourceId} in project {ProjectId}",
                    bulkUploadDto.DataSourceId, projectId);
                // Don't fail the upload because of task creation issues
            }
        }

        return new BulkUploadResultDto
        {
            Results = results,
            TotalFiles = bulkUploadDto.Files.Count,
            SuccessfulUploads = successCount,
            FailedUploads = failureCount,
            Summary = summary
        };
    }

    public async Task<bool> ValidateAssetBelongsToProjectAsync(int assetId, int projectId)
    {
        _logger.LogInformation("Validating that asset {AssetId} belongs to project {ProjectId}", assetId, projectId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found.", assetId);
            return false;
        }

        return asset.ProjectId == projectId;
    }

    /// <summary>
    /// Transfers an asset from its current data source to a target data source.
    /// This involves copying the file in MinIO and updating the database record.
    /// </summary>
    /// <param name="assetId">The ID of the asset to transfer</param>
    /// <param name="targetDataSourceId">The ID of the target data source</param>
    /// <returns>True if the transfer was successful, false otherwise</returns>
    public async Task<bool> TransferAssetToDataSourceAsync(int assetId, int targetDataSourceId)
    {
        try
        {
            // Get the asset first
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset == null)
            {
                _logger.LogError("Asset {AssetId} not found for transfer", assetId);
                return false;
            }

            _logger.LogInformation("Starting asset transfer for asset {AssetId} from data source {CurrentDataSourceId} to {TargetDataSourceId}",
                asset.AssetId, asset.DataSourceId, targetDataSourceId);

            // Skip transfer if asset is already in the target data source
            if (asset.DataSourceId == targetDataSourceId)
            {
                _logger.LogInformation("Asset {AssetId} is already in target data source {DataSourceId}, skipping transfer",
                    asset.AssetId, targetDataSourceId);
                return true;
            }

            // Get both data sources
            var currentDataSource = await _dataSourceRepository.GetByIdAsync(asset.DataSourceId);
            var targetDataSource = await _dataSourceRepository.GetByIdAsync(targetDataSourceId);

            if (currentDataSource == null)
            {
                _logger.LogError("Current data source {DataSourceId} not found for asset {AssetId}",
                    asset.DataSourceId, asset.AssetId);
                return false;
            }

            if (targetDataSource == null)
            {
                _logger.LogError("Target data source {DataSourceId} not found for asset {AssetId}",
                    targetDataSourceId, asset.AssetId);
                return false;
            }

            // Generate bucket names
            var sourceBucketName = _storageService.GenerateBucketName(asset.ProjectId, currentDataSource.Name);
            var targetBucketName = _storageService.GenerateBucketName(asset.ProjectId, targetDataSource.Name);

            _logger.LogDebug("Transferring asset {AssetId} from bucket {SourceBucket} to {TargetBucket}",
                asset.AssetId, sourceBucketName, targetBucketName);

            // Perform the MinIO copy operation
            var copySuccess = await MoveAssetInMinIOAsync(sourceBucketName, targetBucketName, asset.ExternalId);

            if (!copySuccess)
            {
                _logger.LogError("Failed to copy asset {AssetId} in MinIO from {SourceBucket} to {TargetBucket}",
                    asset.AssetId, sourceBucketName, targetBucketName);
                return false;
            }

            // Update the asset's data source in the database
            asset.DataSourceId = targetDataSourceId;
            asset.UpdatedAt = DateTime.UtcNow;

            await _assetRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully transferred asset {AssetId} to data source {TargetDataSourceId}",
                asset.AssetId, targetDataSourceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring asset {AssetId} to data source {TargetDataSourceId}",
                assetId, targetDataSourceId);
            return false;
        }
    }

    #region Private Helper Methods
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

    private static UploadResultDto CreateFailedUploadResult(string filename, string errorMessage, ErrorType errorType)
    {
        return new UploadResultDto
        {
            Filename = filename,
            Success = false,
            ErrorMessage = errorMessage,
            ErrorType = errorType
        };
    }

    private static AssetDto MapToDto(Asset asset, string? imageUrl = null)
    {
        return new AssetDto
        {
            Id = asset.AssetId,
            Filename = asset.Filename,
            MimeType = asset.MimeType,
            SizeBytes = asset.SizeBytes,
            Width = asset.Width,
            Height = asset.Height,
            DurationMs = asset.DurationMs,
            Status = asset.Status,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt,
            ImageUrl = imageUrl
        };
    }

    /// <summary>
    /// Moves an asset file from one MinIO bucket to another.
    /// Uses download and upload since MinIO client doesn't have a direct move method.
    /// </summary>
    /// <param name="sourceBucketName">Source bucket name</param>
    /// <param name="targetBucketName">Target bucket name</param>
    /// <param name="objectName">Object name/path in MinIO</param>
    /// <returns>True if move was successful, false otherwise</returns>
    private async Task<bool> MoveAssetInMinIOAsync(string sourceBucketName, string targetBucketName, string objectName)
    {
        try
        {
            _logger.LogDebug("Moving MinIO object {ObjectName} from bucket {SourceBucket} to {TargetBucket}",
                objectName, sourceBucketName, targetBucketName);

            // Ensure target bucket exists
            if (!await _storageService.BucketExistsAsync(targetBucketName))
            {
                _logger.LogInformation("Creating target bucket {TargetBucket} for asset transfer", targetBucketName);
                await _storageService.CreateBucketAsync(targetBucketName);
            }

            // Check if file exists in source bucket
            if (!await _fileStorageService.FileExistsAsync(sourceBucketName, objectName))
            {
                _logger.LogError("Source file {ObjectName} does not exist in bucket {SourceBucket}",
                    objectName, sourceBucketName);
                return false;
            }

            // Check if file already exists in target bucket
            if (await _fileStorageService.FileExistsAsync(targetBucketName, objectName))
            {
                _logger.LogInformation("File {ObjectName} already exists in target bucket {TargetBucket}, skipping move",
                    objectName, targetBucketName);
                return true;
            }

            // Move file from source bucket to target bucket (copy + delete)
            var targetPath = await _fileStorageService.MoveFileAsync(sourceBucketName, targetBucketName, objectName);

            _logger.LogDebug("Successfully moved asset to target path: {TargetPath}", targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move MinIO object {ObjectName} from {SourceBucket} to {TargetBucket}",
                objectName, sourceBucketName, targetBucketName);
            return false;
        }
    }
    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Triggers automatic task creation for workflow stages that use the specified data source as input
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="dataSourceId">The data source ID that received new assets</param>
    private async System.Threading.Tasks.Task TriggerAutomaticTaskCreationAsync(int projectId, int dataSourceId)
    {
        _logger.LogInformation("Checking for workflow stages that use data source {DataSourceId} as input in project {ProjectId}", 
            dataSourceId, projectId);

        // Find all workflow stages in this project that use this data source as input
        var workflowStages = await _workflowStageRepository.GetAllAsync(
            ws => ws.Workflow.ProjectId == projectId && ws.InputDataSourceId == dataSourceId
        );

        if (!workflowStages.Any())
        {
            _logger.LogInformation("No workflow stages found that use data source {DataSourceId} as input in project {ProjectId}", 
                dataSourceId, projectId);
            return;
        }

        _logger.LogInformation("Found {StageCount} workflow stages using data source {DataSourceId} as input", 
            workflowStages.Count(), dataSourceId);

        foreach (var stage in workflowStages)
        {
            try
            {
                _logger.LogInformation("Creating tasks for workflow stage {StageId} ({StageName}) in workflow {WorkflowId}", 
                    stage.WorkflowStageId, stage.Name, stage.WorkflowId);

                var tasksCreated = await _taskService.CreateTasksForWorkflowStageAsync(
                    projectId, stage.WorkflowId, stage.WorkflowStageId);

                _logger.LogInformation("Created {TasksCreated} tasks for workflow stage {StageId} ({StageName})", 
                    tasksCreated, stage.WorkflowStageId, stage.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tasks for workflow stage {StageId} ({StageName}) in workflow {WorkflowId}", 
                    stage.WorkflowStageId, stage.Name, stage.WorkflowId);
                // Continue with other stages even if one fails
            }
        }
    }

    #endregion
}
