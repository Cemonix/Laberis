using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class ProjectMemberRepository : GenericRepository<ProjectMember>, IProjectMemberRepository
{
    private readonly ILogger<ProjectMemberRepository> _logger;

    public ProjectMemberRepository(LaberisDbContext context, ILogger<ProjectMemberRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<ProjectMember> ApplyIncludes(IQueryable<ProjectMember> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(pm => pm.Project).Include(pm => pm.User);
        return query;
    }

    protected override IQueryable<ProjectMember> ApplyFilter(IQueryable<ProjectMember> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "role":
                if (Enum.TryParse<ProjectRole>(trimmedFilterQuery, true, out var roleEnum))
                {
                    query = query.Where(pm => pm.Role == roleEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project role: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "user_id":
                query = query.Where(pm => pm.UserId == trimmedFilterQuery);
                break;
            case "project_id":
                if (int.TryParse(trimmedFilterQuery, out var projectId))
                {
                    query = query.Where(pm => pm.ProjectId == projectId);
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

    protected override IQueryable<ProjectMember> ApplySorting(IQueryable<ProjectMember> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(pm => pm.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<ProjectMember, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "role":
                keySelector = pm => pm.Role;
                break;
            case "invited_at":
                keySelector = pm => pm.InvitedAt;
                break;
            case "joined_at":
                keySelector = pm => pm.JoinedAt ?? DateTime.MinValue;
                break;
            case "created_at":
                keySelector = pm => pm.CreatedAt;
                break;
            case "updated_at":
                keySelector = pm => pm.UpdatedAt;
                break;
            case "project_member_id":
                keySelector = pm => pm.ProjectMemberId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(pm => pm.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
