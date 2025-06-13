using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class TaskEventRepository : GenericRepository<TaskEvent>, ITaskEventRepository
{
    private readonly ILogger<TaskEventRepository> _logger;

    public TaskEventRepository(LaberisDbContext context, ILogger<TaskEventRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<TaskEvent> ApplyIncludes(IQueryable<TaskEvent> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(te => te.Task).Include(te => te.User).Include(te => te.FromWorkflowStage).Include(te => te.ToWorkflowStage);
        return query;
    }

    protected override IQueryable<TaskEvent> ApplyFilter(IQueryable<TaskEvent> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "event_type":
                if (Enum.TryParse<TaskEventType>(trimmedFilterQuery, true, out var eventTypeEnum))
                {
                    query = query.Where(te => te.EventType == eventTypeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse task event type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "task_id":
                if (int.TryParse(trimmedFilterQuery, out var taskId))
                {
                    query = query.Where(te => te.TaskId == taskId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse task ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "user_id":
                query = query.Where(te => te.UserId == trimmedFilterQuery);
                break;
            case "from_workflow_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var fromStageId))
                {
                    query = query.Where(te => te.FromWorkflowStageId == fromStageId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse from workflow stage ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "to_workflow_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var toStageId))
                {
                    query = query.Where(te => te.ToWorkflowStageId == toStageId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse to workflow stage ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<TaskEvent> ApplySorting(IQueryable<TaskEvent> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first (events are typically viewed chronologically)
            return query.OrderByDescending(te => te.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<TaskEvent, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "created_at":
                keySelector = te => te.CreatedAt;
                break;
            case "event_type":
                keySelector = te => te.EventType;
                break;
            case "task_id":
                keySelector = te => te.TaskId;
                break;
            case "event_id":
                keySelector = te => te.EventId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(te => te.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
