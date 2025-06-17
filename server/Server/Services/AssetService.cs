using server.Models.Domain;
using server.Models.DTOs.Asset;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Models.Domain.Enums;

namespace server.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetRepository assetRepository,
        ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
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
