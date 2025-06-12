using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories
{
    public class LabelRepository : GenericRepository<Label>, ILabelRepository
    {
        private readonly ILogger<LabelRepository> _logger;

        public LabelRepository(LaberisDbContext context, ILogger<LabelRepository> logger) : base(context)
        {
            _logger = logger;
        }

        protected override IQueryable<Label> ApplyFilter(IQueryable<Label> query, string? filterOn, string? filterQuery)
        {
            if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
            {
                return query;
            }

            if (filterOn.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => EF.Functions.ILike(l.Name, $"%{filterQuery.Trim()}%"));
            }

            return query;
        }

        protected override IQueryable<Label> ApplyIncludes(IQueryable<Label> query)
        {
            return query.Include(l => l.LabelScheme);
        }

        protected override IQueryable<Label> ApplySorting(IQueryable<Label> query, string? sortBy, bool isAscending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query.OrderBy(l => l.Name);
            }

            Expression<Func<Label, object>> keySelector = sortBy.ToLowerInvariant() switch
            {
                "name" => l => l.Name,
                "created_at" => l => l.CreatedAt,
                _ => l => l.LabelId
            };

            return isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
    }
}