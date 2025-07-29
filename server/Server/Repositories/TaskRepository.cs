using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;
using System.Linq.Expressions;
using LaberisTask = server.Models.Domain.Task;

namespace server.Repositories;

public class TaskRepository : GenericRepository<LaberisTask>, ITaskRepository
{
    private readonly ILogger<TaskRepository> _logger;

    public TaskRepository(LaberisDbContext context, ILogger<TaskRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<LaberisTask> ApplyIncludes(IQueryable<LaberisTask> query)
    {
        // Include related data needed for status calculation and user info
        return query
            .Include(t => t.CurrentWorkflowStage)
            .Include(t => t.AssignedToUser)
            .Include(t => t.LastWorkedOnByUser);
    }

    protected override IQueryable<LaberisTask> ApplyFilter(IQueryable<LaberisTask> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "priority":
                if (int.TryParse(trimmedFilterQuery, out var priority))
                {
                    query = query.Where(t => t.Priority == priority);
                }
                else
                {
                    _logger.LogWarning("Failed to parse priority: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "assigned_to_user_id":
                query = query.Where(t => t.AssignedToUserId == trimmedFilterQuery);
                break;
            case "last_worked_on_by_user_id":
                query = query.Where(t => t.LastWorkedOnByUserId == trimmedFilterQuery);
                break;
            case "project_id":
                if (int.TryParse(trimmedFilterQuery, out var projectId))
                {
                    query = query.Where(t => t.ProjectId == projectId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "workflow_id":
                if (int.TryParse(trimmedFilterQuery, out var workflowId))
                {
                    query = query.Where(t => t.WorkflowId == workflowId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "current_workflow_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var stageId))
                {
                    query = query.Where(t => t.CurrentWorkflowStageId == stageId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow stage ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "is_completed":
                var isCompleted = trimmedFilterQuery.ToLowerInvariant() == "true";
                if (isCompleted)
                {
                    query = query.Where(t => t.CompletedAt != null);
                }
                else
                {
                    query = query.Where(t => t.CompletedAt == null);
                }
                break;
            case "is_archived":
                var isArchived = trimmedFilterQuery.ToLowerInvariant() == "true";
                if (isArchived)
                {
                    query = query.Where(t => t.ArchivedAt != null);
                }
                else
                {
                    query = query.Where(t => t.ArchivedAt == null);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<LaberisTask> ApplySorting(IQueryable<LaberisTask> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: highest priority first, then newest
            return query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<LaberisTask, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "priority":
                keySelector = t => t.Priority;
                break;
            case "due_date":
                keySelector = t => t.DueDate ?? DateTime.MaxValue;
                break;
            case "created_at":
                keySelector = t => t.CreatedAt;
                break;
            case "updated_at":
                keySelector = t => t.UpdatedAt;
                break;
            case "completed_at":
                keySelector = t => t.CompletedAt ?? DateTime.MaxValue;
                break;
            case "archived_at":
                keySelector = t => t.ArchivedAt ?? DateTime.MaxValue;
                break;
            case "task_id":
                keySelector = t => t.TaskId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }

    public async Task<IEnumerable<Asset>> GetAvailableAssetsForTaskCreationAsync(int projectId)
    {
        _logger.LogInformation("Getting available assets for task creation in project {ProjectId}", projectId);

        // Get all assets in the project that are imported and don't have active (uncomplete) tasks
        var availableAssets = await _context.Assets
            .Where(a => a.ProjectId == projectId 
                       && a.Status == Models.Domain.Enums.AssetStatus.IMPORTED
                       && !_context.Tasks.Any(t => t.AssetId == a.AssetId && t.CompletedAt == null))
            .ToListAsync();

        _logger.LogInformation("Found {Count} available assets for task creation in project {ProjectId}", 
            availableAssets.Count, projectId);

        return availableAssets;
    }

    public async Task<int> GetAvailableAssetsCountAsync(int projectId, int? dataSourceId = null)
    {
        _logger.LogInformation("Getting available assets count for project {ProjectId}, dataSource {DataSourceId}", 
            projectId, dataSourceId);

        var query = _context.Assets
            .Where(a => a.ProjectId == projectId 
                       && a.Status == Models.Domain.Enums.AssetStatus.IMPORTED
                       && !_context.Tasks.Any(t => t.AssetId == a.AssetId && t.CompletedAt == null));

        if (dataSourceId.HasValue)
        {
            query = query.Where(a => a.DataSourceId == dataSourceId.Value);
        }

        var count = await query.CountAsync();

        _logger.LogInformation("Found {Count} available assets in project {ProjectId}, dataSource {DataSourceId}", 
            count, projectId, dataSourceId);

        return count;
    }

    public async Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId)
    {
        _logger.LogInformation("Getting available assets from data source {DataSourceId} in project {ProjectId}", 
            dataSourceId, projectId);

        var availableAssets = await _context.Assets
            .Where(a => a.ProjectId == projectId 
                       && a.DataSourceId == dataSourceId
                       && a.Status == Models.Domain.Enums.AssetStatus.IMPORTED
                       && !_context.Tasks.Any(t => t.AssetId == a.AssetId && t.CompletedAt == null))
            .ToListAsync();

        _logger.LogInformation("Found {Count} available assets from data source {DataSourceId} in project {ProjectId}", 
            availableAssets.Count, dataSourceId, projectId);

        return availableAssets;
    }

    public async Task<WorkflowStage?> GetNextWorkflowStageAsync(int currentStageId, string? condition = null)
    {
        _logger.LogInformation("Finding next workflow stage for stage {CurrentStageId} with condition '{Condition}'", 
            currentStageId, condition ?? "none");

        WorkflowStage? nextStage = null;
        
        if (!string.IsNullOrEmpty(condition))
        {
            // First, try to find a connection with the exact condition
            nextStage = await _context.WorkflowStageConnections
                .Where(c => c.FromStageId == currentStageId && c.Condition == condition)
                .Select(c => c.ToStage)
                .FirstOrDefaultAsync();
                
            if (nextStage != null)
            {
                _logger.LogInformation("Found next stage via condition-based routing: {NextStageId} with condition '{Condition}'", 
                    nextStage.WorkflowStageId, condition);
            }
        }
        
        // If no stage found with specific condition, look for default connection (no condition)
        if (nextStage == null)
        {
            nextStage = await _context.WorkflowStageConnections
                .Where(c => c.FromStageId == currentStageId && c.Condition == null)
                .Select(c => c.ToStage)
                .FirstOrDefaultAsync();
                
            if (nextStage != null)
            {
                _logger.LogInformation("Found next stage via default routing: {NextStageId} (no condition)", 
                    nextStage.WorkflowStageId);
            }
        }

        if (nextStage != null)
        {
            _logger.LogInformation("Found next stage: {NextStageId} ({NextStageName})", 
                nextStage.WorkflowStageId, nextStage.Name);
        }
        else
        {
            _logger.LogInformation("No next stage found for stage {CurrentStageId}", currentStageId);
        }

        return nextStage;
    }
}
