using server.Models.DTOs.Annotation;

namespace server.Services.Interfaces;

public interface IAnnotationService
{
    /// <summary>
    /// Retrieves all annotations for a specific task, optionally filtered and sorted.
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve annotations for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "annotation_type", "is_prediction", "annotator_user_id").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "confidence_score", "version").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of AnnotationDto.</returns>
    Task<IEnumerable<AnnotationDto>> GetAnnotationsForTaskAsync(
        int taskId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves all annotations for a specific asset, optionally filtered and sorted.
    /// </summary>
    /// <param name="assetId">The ID of the asset to retrieve annotations for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "annotation_type", "is_prediction", "annotator_user_id").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "confidence_score", "version").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of AnnotationDto.</returns>
    Task<IEnumerable<AnnotationDto>> GetAnnotationsForAssetAsync(
        int assetId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves an annotation by its ID.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the AnnotationDto if found, otherwise null.</returns>
    Task<AnnotationDto?> GetAnnotationByIdAsync(long annotationId);

    /// <summary>
    /// Creates a new annotation.
    /// </summary>
    /// <param name="createDto">The DTO containing information for the new annotation.</param>
    /// <param name="annotatorUserId">The ID of the user creating the annotation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created AnnotationDto.</returns>
    Task<AnnotationDto> CreateAnnotationAsync(CreateAnnotationDto createDto, string annotatorUserId);

    /// <summary>
    /// Updates an existing annotation.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation to update.</param>
    /// <param name="updateDto">The DTO containing updated annotation information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated AnnotationDto if successful, otherwise null.</returns>
    Task<AnnotationDto?> UpdateAnnotationAsync(long annotationId, UpdateAnnotationDto updateDto);

    /// <summary>
    /// Deletes an annotation by its ID.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the annotation was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteAnnotationAsync(long annotationId);
}
