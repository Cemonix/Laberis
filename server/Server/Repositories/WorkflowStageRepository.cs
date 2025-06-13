using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class WorkflowStageRepository : GenericRepository<WorkflowStage>, IWorkflowStageRepository
{
    private readonly ILogger<WorkflowStageRepository> _logger;

    public WorkflowStageRepository(LaberisDbContext context, ILogger<WorkflowStageRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<WorkflowStage> ApplyIncludes(IQueryable<WorkflowStage> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(ws => ws.Workflow).Include(ws => ws.InputDataSource).Include(ws => ws.TargetDataSource);
        return query;
    }

    protected override IQueryable<WorkflowStage> ApplyFilter(IQueryable<WorkflowStage> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "name":
                query = query.Where(ws => EF.Functions.ILike(ws.Name, $"%{trimmedFilterQuery}%"));
                break;
            case "description":
                query = query.Where(ws => EF.Functions.ILike(ws.Description ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "stage_type":
                if (Enum.TryParse<WorkflowStageType>(trimmedFilterQuery, true, out var stageTypeEnum))
                {
                    query = query.Where(ws => ws.StageType == stageTypeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow stage type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "workflow_id":
                if (int.TryParse(trimmedFilterQuery, out var workflowId))
                {
                    query = query.Where(ws => ws.WorkflowId == workflowId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "is_initial_stage":
                if (bool.TryParse(trimmedFilterQuery, out var isInitial))
                {
                    query = query.Where(ws => ws.IsInitialStage == isInitial);
                }
                else
                {
                    _logger.LogWarning("Failed to parse is_initial_stage boolean: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "is_final_stage":
                if (bool.TryParse(trimmedFilterQuery, out var isFinal))
                {
                    query = query.Where(ws => ws.IsFinalStage == isFinal);
                }
                else
                {
                    _logger.LogWarning("Failed to parse is_final_stage boolean: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<WorkflowStage> ApplySorting(IQueryable<WorkflowStage> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: by stage order within workflow
            return query.OrderBy(ws => ws.WorkflowId).ThenBy(ws => ws.StageOrder);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<WorkflowStage, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "name":
                keySelector = ws => ws.Name;
                break;
            case "stage_order":
                keySelector = ws => ws.StageOrder;
                break;
            case "created_at":
                keySelector = ws => ws.CreatedAt;
                break;
            case "updated_at":
                keySelector = ws => ws.UpdatedAt;
                break;
            case "stage_type":
                keySelector = ws => ws.StageType ?? WorkflowStageType.ANNOTATION;
                break;
            case "workflow_stage_id":
                keySelector = ws => ws.WorkflowStageId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderBy(ws => ws.WorkflowId).ThenBy(ws => ws.StageOrder);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
