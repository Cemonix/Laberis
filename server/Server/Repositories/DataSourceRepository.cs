using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class DataSourceRepository : GenericRepository<DataSource>, IDataSourceRepository
{
    private readonly ILogger<DataSourceRepository> _logger;

    public DataSourceRepository(LaberisDbContext context, ILogger<DataSourceRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<DataSource> ApplyFilter(IQueryable<DataSource> query, string? filterOn, string? filterQuery)
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
                query = query.Where(ds => EF.Functions.ILike(ds.Name, $"%{trimmedFilterQuery}%"));
                break;
            case "source_type":
                if (Enum.TryParse<DataSourceType>(trimmedFilterQuery, true, out var sourceTypeEnum))
                {
                    query = query.Where(ds => ds.SourceType == sourceTypeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse data source type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property for DataSource: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<DataSource> ApplyIncludes(IQueryable<DataSource> query)
    {
        // TODO: No default includes needed for now, but could include Project if necessary.
        // return query.Include(ds => ds.Project);
        return query;
    }

    protected override IQueryable<DataSource> ApplySorting(IQueryable<DataSource> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(ds => ds.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<DataSource, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "name":
                keySelector = ds => ds.Name;
                break;
            case "created_at":
                keySelector = ds => ds.CreatedAt;
                break;
            default:
                _logger.LogWarning("Unknown sort property for DataSource: {SortBy}", sortBy);
                return query.OrderByDescending(ds => ds.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}