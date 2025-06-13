using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class WorkflowRepository : GenericRepository<Workflow>, IWorkflowRepository
{
    private readonly ILogger<WorkflowRepository> _logger;

    public WorkflowRepository(LaberisDbContext context, ILogger<WorkflowRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Workflow> ApplyIncludes(IQueryable<Workflow> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(w => w.Project).Include(w => w.WorkflowStages);
        return query;
    }

    protected override IQueryable<Workflow> ApplyFilter(IQueryable<Workflow> query, string? filterOn, string? filterQuery)
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
                query = query.Where(w => EF.Functions.ILike(w.Name, $"%{trimmedFilterQuery}%"));
                break;
            case "description":
                query = query.Where(w => EF.Functions.ILike(w.Description ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "is_default":
                if (bool.TryParse(trimmedFilterQuery, out var isDefault))
                {
                    query = query.Where(w => w.IsDefault == isDefault);
                }
                else
                {
                    _logger.LogWarning("Failed to parse is_default boolean: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "project_id":
                if (int.TryParse(trimmedFilterQuery, out var projectId))
                {
                    query = query.Where(w => w.ProjectId == projectId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Workflow> ApplySorting(IQueryable<Workflow> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: default workflows first, then by name
            return query.OrderByDescending(w => w.IsDefault).ThenBy(w => w.Name);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Workflow, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "name":
                keySelector = w => w.Name;
                break;
            case "created_at":
                keySelector = w => w.CreatedAt;
                break;
            case "updated_at":
                keySelector = w => w.UpdatedAt;
                break;
            case "is_default":
                keySelector = w => w.IsDefault;
                break;
            case "workflow_id":
                keySelector = w => w.WorkflowId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(w => w.IsDefault).ThenBy(w => w.Name);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
