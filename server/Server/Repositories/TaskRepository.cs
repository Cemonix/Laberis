using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
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

    /// <summary>
    /// Override GetByIdAsync to ensure navigation properties are loaded
    /// </summary>
    public override async Task<LaberisTask?> GetByIdAsync(object id)
    {
        return await _dbSet
            .Include(t => t.CurrentWorkflowStage)
            .Include(t => t.AssignedToUser)
            .Include(t => t.LastWorkedOnByUser)
            .FirstOrDefaultAsync(t => t.TaskId == (int)id);
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
            case "duedate":
                keySelector = t => t.DueDate ?? DateTime.MaxValue;
                break;
            case "created_at":
            case "createdat":
                keySelector = t => t.CreatedAt;
                break;
            case "updated_at":
            case "updatedat":
                keySelector = t => t.UpdatedAt;
                break;
            case "completed_at":
            case "completedat":
                keySelector = t => t.CompletedAt ?? DateTime.MaxValue;
                break;
            case "archived_at":
            case "archivedat":
                keySelector = t => t.ArchivedAt ?? DateTime.MaxValue;
                break;
            case "task_id":
            case "taskid":
            case "id":
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
}
