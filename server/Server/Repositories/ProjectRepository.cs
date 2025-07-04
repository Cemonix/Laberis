using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;

namespace server.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    private readonly ILogger<ProjectRepository> _logger;

    public ProjectRepository(LaberisDbContext context, ILogger<ProjectRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Project> ApplyIncludes(IQueryable<Project> query)
    {
        // TODO: If specific use cases need related data (e.g., Owner), they can use specific methods
        // or this can be expanded.
        // Example: return query.Include(p => p.Owner);
        return query;
    }

    protected override IQueryable<Project> ApplyFilter(
        IQueryable<Project> query, string? filterOn, string? filterQuery
    )
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        // Normalize for case-insensitive matching of filterOn property name
        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "name":
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{trimmedFilterQuery}%"));
                break;
            case "project_type":
                if (Enum.TryParse<ProjectType>(trimmedFilterQuery, true, out var projectTypeEnum))
                {
                    query = query.Where(p => p.ProjectType == projectTypeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "status":
                if (Enum.TryParse<ProjectStatus>(trimmedFilterQuery, true, out var projectStatusEnum))
                {
                    query = query.Where(p => p.Status == projectStatusEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project status: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Project> ApplySorting(IQueryable<Project> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(p => p.CreatedAt);
        }

        // Normalize for case-insensitive matching of sortBy property name
        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Project, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "name":
                keySelector = p => p.Name;
                break;
            case "created_at":
                keySelector = p => p.CreatedAt;
                break;
            case "project_type":
                keySelector = p => p.ProjectType;
                break;
            case "status":
                keySelector = p => p.Status;
                break;
            case "project_id":
                keySelector = p => p.ProjectId;
                 break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(p => p.CreatedAt);
        }
        
        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
