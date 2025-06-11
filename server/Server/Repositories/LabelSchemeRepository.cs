using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class LabelSchemeRepository : GenericRepository<LabelScheme>, ILabelSchemeRepository
{
    private readonly ILogger<LabelSchemeRepository> _logger;

    public LabelSchemeRepository(LaberisDbContext context, ILogger<LabelSchemeRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<LabelScheme> ApplyFilter(IQueryable<LabelScheme> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        if (filterOn.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ls => EF.Functions.ILike(ls.Name, $"%{filterQuery.Trim()}%"));
        }

        return query;
    }

    protected override IQueryable<LabelScheme> ApplyIncludes(IQueryable<LabelScheme> query)
    {
        // No includes needed by default for label schemes.
        return query;
    }

    protected override IQueryable<LabelScheme> ApplySorting(IQueryable<LabelScheme> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort by name
            return query.OrderBy(ls => ls.Name);
        }

        Expression<Func<LabelScheme, object>> keySelector = sortBy.ToLowerInvariant() switch
        {
            "name" => ls => ls.Name,
            "created_at" => ls => ls.CreatedAt,
            _ => ls => ls.LabelSchemeId
        };

        query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        
        return query;
    }
}