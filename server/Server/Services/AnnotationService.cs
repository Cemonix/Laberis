using server.Models.Domain;
using server.Models.DTOs.Annotation;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class AnnotationService : IAnnotationService
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<AnnotationService> _logger;

    public AnnotationService(
        IAnnotationRepository annotationRepository,
        ILogger<AnnotationService> logger)
    {
        _annotationRepository = annotationRepository ?? throw new ArgumentNullException(nameof(annotationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<AnnotationDto>> GetAnnotationsForTaskAsync(
        int taskId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching annotations for task: {TaskId}", taskId);

        var (annotations, totalCount) = await _annotationRepository.GetAllWithCountAsync(
            filter: a => a.TaskId == taskId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} annotations for task: {TaskId}", annotations.Count(), taskId);

        var annotationDtos = annotations.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AnnotationDto>
        {
            Data = annotationDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<PaginatedResponse<AnnotationDto>> GetAnnotationsForAssetAsync(
        int assetId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching annotations for asset: {AssetId}", assetId);

        var (annotations, totalCount) = await _annotationRepository.GetAllWithCountAsync(
            filter: a => a.AssetId == assetId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} annotations for asset: {AssetId}", annotations.Count(), assetId);

        var annotationDtos = annotations.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AnnotationDto>
        {
            Data = annotationDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<AnnotationDto?> GetAnnotationByIdAsync(long annotationId)
    {
        _logger.LogInformation("Fetching annotation with ID: {AnnotationId}", annotationId);

        var annotation = await _annotationRepository.GetByIdAsync(annotationId);

        if (annotation == null)
        {
            _logger.LogWarning("Annotation with ID: {AnnotationId} not found.", annotationId);
            return null;
        }

        _logger.LogInformation("Successfully fetched annotation with ID: {AnnotationId}", annotationId);
        return MapToDto(annotation);
    }

    public async Task<AnnotationDto> CreateAnnotationAsync(CreateAnnotationDto createDto, string annotatorUserId)
    {
        _logger.LogInformation("Creating new annotation for task: {TaskId}, asset: {AssetId}",
            createDto.TaskId, createDto.AssetId);

        var annotation = new Annotation
        {
            AnnotationType = createDto.AnnotationType,
            Data = createDto.Data,
            IsPrediction = createDto.IsPrediction,
            ConfidenceScore = createDto.ConfidenceScore,
            IsGroundTruth = createDto.IsGroundTruth,
            Version = 1, // Default version for new annotations
            Notes = createDto.Notes,
            TaskId = createDto.TaskId,
            AssetId = createDto.AssetId,
            LabelId = createDto.LabelId,
            AnnotatorUserId = annotatorUserId,
            ParentAnnotationId = createDto.ParentAnnotationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _annotationRepository.AddAsync(annotation);
        await _annotationRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created annotation with ID: {AnnotationId}", annotation.AnnotationId);

        return MapToDto(annotation);
    }

    public async Task<AnnotationDto?> UpdateAnnotationAsync(long annotationId, UpdateAnnotationDto updateDto)
    {
        _logger.LogInformation("Updating annotation with ID: {AnnotationId}", annotationId);

        var existingAnnotation = await _annotationRepository.GetByIdAsync(annotationId);
        if (existingAnnotation == null)
        {
            _logger.LogWarning("Annotation with ID: {AnnotationId} not found for update.", annotationId);
            return null;
        }

        // Create a new entity with updated values
        var updatedAnnotation = existingAnnotation with
        {
            Data = updateDto.Data ?? existingAnnotation.Data,
            LabelId = updateDto.LabelId ?? existingAnnotation.LabelId,
            IsPrediction = updateDto.IsPrediction ?? existingAnnotation.IsPrediction,
            ConfidenceScore = updateDto.ConfidenceScore ?? existingAnnotation.ConfidenceScore,
            IsGroundTruth = updateDto.IsGroundTruth ?? existingAnnotation.IsGroundTruth,
            Notes = updateDto.Notes ?? existingAnnotation.Notes,
            UpdatedAt = DateTime.UtcNow
        };

        // Detach the existing entity to avoid tracking conflicts
        _annotationRepository.Detach(existingAnnotation);
        _annotationRepository.Update(updatedAnnotation);
        await _annotationRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated annotation with ID: {AnnotationId}", annotationId);
        return MapToDto(updatedAnnotation);
    }

    public async Task<bool> DeleteAnnotationAsync(long annotationId)
    {
        _logger.LogInformation("Deleting annotation with ID: {AnnotationId}", annotationId);

        var annotation = await _annotationRepository.GetByIdAsync(annotationId);

        if (annotation == null)
        {
            _logger.LogWarning("Annotation with ID: {AnnotationId} not found for deletion.", annotationId);
            return false;
        }

        _annotationRepository.Remove(annotation);
        await _annotationRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted annotation with ID: {AnnotationId}", annotationId);
        return true;
    }

    private static AnnotationDto MapToDto(Annotation annotation)
    {
        return new AnnotationDto
        {
            Id = annotation.AnnotationId,
            AnnotationType = annotation.AnnotationType,
            Data = annotation.Data,
            IsPrediction = annotation.IsPrediction,
            ConfidenceScore = annotation.ConfidenceScore,
            IsGroundTruth = annotation.IsGroundTruth,
            Version = annotation.Version,
            Notes = annotation.Notes,
            TaskId = annotation.TaskId,
            AssetId = annotation.AssetId,
            LabelId = annotation.LabelId,
            AnnotatorEmail = annotation.AnnotatorUser?.Email ?? string.Empty,
            ParentAnnotationId = annotation.ParentAnnotationId,
            CreatedAt = annotation.CreatedAt,
            UpdatedAt = annotation.UpdatedAt
        };
    }
}
