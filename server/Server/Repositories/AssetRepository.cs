using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class AssetRepository : GenericRepository<Asset>, IAssetRepository
{
    private readonly ILogger<AssetRepository> _logger;

    public AssetRepository(LaberisDbContext context, ILogger<AssetRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Asset> ApplyIncludes(IQueryable<Asset> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(a => a.Project).Include(a => a.DataSource);
        return query;
    }

    protected override IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "filename":
                query = query.Where(a => EF.Functions.ILike(a.Filename ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "external_id":
                query = query.Where(a => EF.Functions.ILike(a.ExternalId, $"%{trimmedFilterQuery}%"));
                break;
            case "mime_type":
                query = query.Where(a => EF.Functions.ILike(a.MimeType ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "status":
                if (Enum.TryParse<AssetStatus>(trimmedFilterQuery, true, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse asset status: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Asset> ApplySorting(IQueryable<Asset> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(a => a.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Asset, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "filename":
                keySelector = a => a.Filename ?? "";
                break;
            case "external_id":
                keySelector = a => a.ExternalId;
                break;
            case "created_at":
                keySelector = a => a.CreatedAt;
                break;
            case "updated_at":
                keySelector = a => a.UpdatedAt;
                break;
            case "status":
                keySelector = a => a.Status;
                break;
            case "size_bytes":
                keySelector = a => a.SizeBytes ?? 0;
                break;
            case "asset_id":
                keySelector = a => a.AssetId;
                break;
            default:
                _logger.LogWarning("Unknown sort property: {SortBy}", sortBy);
                return query.OrderByDescending(a => a.CreatedAt);
        }

        if (keySelector != null)
        {
            query = isAscending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }
        return query;
    }
}
