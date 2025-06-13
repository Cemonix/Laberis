using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class IssueRepository : GenericRepository<Issue>, IIssueRepository
{
    private readonly ILogger<IssueRepository> _logger;

    public IssueRepository(LaberisDbContext context, ILogger<IssueRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Issue> ApplyIncludes(IQueryable<Issue> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(i => i.Task).Include(i => i.Asset).Include(i => i.ReportedByUser);
        return query;
    }

    protected override IQueryable<Issue> ApplyFilter(IQueryable<Issue> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "description":
                query = query.Where(i => EF.Functions.ILike(i.Description, $"%{trimmedFilterQuery}%"));
                break;
            case "status":
                if (Enum.TryParse<IssueStatus>(trimmedFilterQuery, true, out var statusEnum))
                {
                    query = query.Where(i => i.Status == statusEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse issue status: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "issue_type":
                if (Enum.TryParse<IssueType>(trimmedFilterQuery, true, out var typeEnum))
                {
                    query = query.Where(i => i.IssueType == typeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse issue type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "priority":
                if (int.TryParse(trimmedFilterQuery, out var priority))
                {
                    query = query.Where(i => i.Priority == priority);
                }
                else
                {
                    _logger.LogWarning("Failed to parse priority: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "reported_by_user_id":
                query = query.Where(i => i.ReportedByUserId == trimmedFilterQuery);
                break;
            case "assigned_to_user_id":
                query = query.Where(i => i.AssignedToUserId == trimmedFilterQuery);
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Issue> ApplySorting(IQueryable<Issue> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(i => i.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Issue, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "created_at":
                keySelector = i => i.CreatedAt;
                break;
            case "updated_at":
                keySelector = i => i.UpdatedAt;
                break;
            case "status":
                keySelector = i => i.Status;
                break;
            case "priority":
                keySelector = i => i.Priority;
                break;
            case "issue_type":
                keySelector = i => i.IssueType ?? IssueType.OTHER;
                break;
            case "resolved_at":
                keySelector = i => i.ResolvedAt ?? DateTime.MinValue;
                break;
            case "issue_id":
                keySelector = i => i.IssueId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(i => i.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
