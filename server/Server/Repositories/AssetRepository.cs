using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class AssetRepository : GenericRepository<Asset>, IAssetRepository
{
    private readonly ILogger<AssetRepository> _logger;

    public AssetRepository(LaberisDbContext context, ILogger<AssetRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Asset> ApplyIncludes(IQueryable<Asset> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(a => a.Project).Include(a => a.DataSource);
        return query;
    }

    protected override IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "filename":
                query = query.Where(a => EF.Functions.ILike(a.Filename ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "external_id":
                query = query.Where(a => EF.Functions.ILike(a.ExternalId, $"%{trimmedFilterQuery}%"));
                break;
            case "mime_type":
                query = query.Where(a => EF.Functions.ILike(a.MimeType ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "status":
                if (Enum.TryParse<AssetStatus>(trimmedFilterQuery, true, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse asset status: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Asset> ApplySorting(IQueryable<Asset> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(a => a.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Asset, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "filename":
                keySelector = a => a.Filename ?? "";
                break;
            case "external_id":
                keySelector = a => a.ExternalId;
                break;
            case "created_at":
                keySelector = a => a.CreatedAt;
                break;
            case "updated_at":
                keySelector = a => a.UpdatedAt;
                break;
            case "status":
                keySelector = a => a.Status;
                break;
            case "size_bytes":
                keySelector = a => a.SizeBytes ?? 0;
                break;
            case "asset_id":
                keySelector = a => a.AssetId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(a => a.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }

    public async Task<int> GetAvailableAssetsCountAsync(int projectId)
    {
        _logger.LogInformation("Getting available assets count for project {ProjectId}", projectId);

        var query = _context.Assets
            .Where(a => a.ProjectId == projectId
                && a.Status == AssetStatus.IMPORTED
                && !_context.Tasks.Any(t => t.AssetId == a.AssetId));

        var count = await query.CountAsync();

        _logger.LogInformation("Found {Count} available assets in project {ProjectId}",
            count, projectId);

        return count;
    }

    public async Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId)
    {
        _logger.LogInformation("Getting available assets from data source {DataSourceId} in project {ProjectId}",
            dataSourceId, projectId);

        var availableAssets = await _context.Assets
            .Where(a => a.ProjectId == projectId
                && a.DataSourceId == dataSourceId
                && a.Status == AssetStatus.IMPORTED
                && !_context.Tasks.Any(t => t.AssetId == a.AssetId))
            .ToListAsync();

        _logger.LogInformation("Found {Count} available assets in data source {DataSourceId} in project {ProjectId}",
            availableAssets.Count, dataSourceId, projectId);

        return availableAssets;
    }
    
    public async Task<IEnumerable<Asset>> GetAvailableAssetsForTaskCreationAsync(int projectId, int? workflowStageId = null)
    {
        _logger.LogInformation("Getting available assets for task creation in project {ProjectId} for workflow stage {WorkflowStageId}", 
            projectId, workflowStageId);

        var query = _context.Assets
            .Where(a => a.ProjectId == projectId 
                && a.Status == AssetStatus.IMPORTED
                && !_context.Tasks.Any(t => t.AssetId == a.AssetId));

        // If a workflow stage is specified, filter assets to only those in the stage's input data source
        if (workflowStageId.HasValue)
        {
            var workflowStage = await _context.WorkflowStages
                .FirstOrDefaultAsync(ws => ws.WorkflowStageId == workflowStageId.Value);

            if (workflowStage?.InputDataSourceId.HasValue == true)
            {
                _logger.LogInformation("Filtering assets by input data source {DataSourceId} for workflow stage {WorkflowStageId}", 
                    workflowStage.InputDataSourceId, workflowStageId);
                    
                query = query.Where(a => a.DataSourceId == workflowStage.InputDataSourceId.Value);
            }
            else
            {
                _logger.LogWarning("Workflow stage {WorkflowStageId} has no input data source configured, attempting fallback to project's default data source", workflowStageId);
                
                // Fallback: Find the project's default data source (usually the first one created for the project)
                var defaultDataSource = await _context.DataSources
                    .Where(ds => ds.ProjectId == projectId)
                    .OrderBy(ds => ds.CreatedAt)
                    .FirstOrDefaultAsync();
                
                if (defaultDataSource != null)
                {
                    _logger.LogInformation("Using fallback data source {DataSourceId} for workflow stage {WorkflowStageId}", 
                        defaultDataSource.DataSourceId, workflowStageId);
                    query = query.Where(a => a.DataSourceId == defaultDataSource.DataSourceId);
                }
                else
                {
                    _logger.LogError("No data sources found for project {ProjectId}, cannot create tasks for workflow stage {WorkflowStageId}", 
                        projectId, workflowStageId);
                    return [];
                }
            }
        }

        var availableAssets = await query.ToListAsync();

        _logger.LogInformation("Found {Count} available assets for task creation in project {ProjectId} for workflow stage {WorkflowStageId}", 
            availableAssets.Count, projectId, workflowStageId);

        return availableAssets;
    }
}
