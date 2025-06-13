using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Repositories;

public class AnnotationRepository : GenericRepository<Annotation>, IAnnotationRepository
{
    private readonly ILogger<AnnotationRepository> _logger;

    public AnnotationRepository(LaberisDbContext context, ILogger<AnnotationRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<Annotation> ApplyIncludes(IQueryable<Annotation> query)
    {
        // Include related data if needed for specific use cases
        // Example: return query.Include(a => a.Task).Include(a => a.Asset).Include(a => a.Label);
        return query;
    }

    protected override IQueryable<Annotation> ApplyFilter(IQueryable<Annotation> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "annotation_type":
                if (Enum.TryParse<AnnotationType>(trimmedFilterQuery, true, out var typeEnum))
                {
                    query = query.Where(a => a.AnnotationType == typeEnum);
                }
                else
                {
                    _logger.LogWarning("Failed to parse annotation type: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "is_prediction":
                if (bool.TryParse(trimmedFilterQuery, out var isPrediction))
                {
                    query = query.Where(a => a.IsPrediction == isPrediction);
                }
                else
                {
                    _logger.LogWarning("Failed to parse is_prediction boolean: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "is_ground_truth":
                if (bool.TryParse(trimmedFilterQuery, out var isGroundTruth))
                {
                    query = query.Where(a => a.IsGroundTruth == isGroundTruth);
                }
                else
                {
                    _logger.LogWarning("Failed to parse is_ground_truth boolean: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "annotator_user_id":
                query = query.Where(a => a.AnnotatorUserId == trimmedFilterQuery);
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<Annotation> ApplySorting(IQueryable<Annotation> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sort: newest first
            return query.OrderByDescending(a => a.CreatedAt);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        Expression<Func<Annotation, object>>? keySelector = null;

        switch (normalizedSortBy)
        {
            case "created_at":
                keySelector = a => a.CreatedAt;
                break;
            case "updated_at":
                keySelector = a => a.UpdatedAt;
                break;
            case "annotation_type":
                keySelector = a => a.AnnotationType;
                break;
            case "confidence_score":
                keySelector = a => a.ConfidenceScore ?? 0;
                break;
            case "version":
                keySelector = a => a.Version;
                break;
            case "annotation_id":
                keySelector = a => a.AnnotationId;
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
