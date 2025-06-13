using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.Annotation;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnnotationsController : ControllerBase
{
    private readonly IAnnotationService _annotationService;
    private readonly ILogger<AnnotationsController> _logger;

    public AnnotationsController(IAnnotationService annotationService, ILogger<AnnotationsController> logger)
    {
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all annotations for a specific task with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="taskId">The ID of the task to get annotations for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "annotation_type", "is_prediction", "annotator_user_id").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "confidence_score", "version").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of annotation DTOs.</returns>
    /// <response code="200">Returns the list of annotation DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("task/{taskId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AnnotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnnotationsForTask(
        int taskId,
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        try
        {
            var annotations = await _annotationService.GetAnnotationsForTaskAsync(
                taskId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching annotations for task {TaskId}.", taskId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all annotations for a specific asset with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="assetId">The ID of the asset to get annotations for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "annotation_type", "is_prediction", "annotator_user_id").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "confidence_score", "version").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of annotation DTOs.</returns>
    /// <response code="200">Returns the list of annotation DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("asset/{assetId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AnnotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnnotationsForAsset(
        int assetId,
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        try
        {
            var annotations = await _annotationService.GetAnnotationsForAssetAsync(
                assetId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching annotations for asset {AssetId}.", assetId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific annotation by its unique ID.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation.</param>
    /// <returns>The requested annotation.</returns>
    [HttpGet("{annotationId:long}")]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnnotationById(long annotationId)
    {
        try
        {
            var annotation = await _annotationService.GetAnnotationByIdAsync(annotationId);

            if (annotation == null)
            {
                return NotFound($"Annotation with ID {annotationId} not found.");
            }

            return Ok(annotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching annotation {AnnotationId}.", annotationId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new annotation.
    /// </summary>
    /// <param name="createAnnotationDto">The annotation creation data.</param>
    /// <returns>The newly created annotation.</returns>
    /// <response code="201">Returns the newly created annotation and its location.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAnnotation([FromBody] CreateAnnotationDto createAnnotationDto)
    {
        var annotatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(annotatorUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var newAnnotation = await _annotationService.CreateAnnotationAsync(createAnnotationDto, annotatorUserId);
            return CreatedAtAction(nameof(GetAnnotationById), 
                new { annotationId = newAnnotation.Id }, newAnnotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating annotation for task {TaskId}.", createAnnotationDto.TaskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Updates an existing annotation.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation to update.</param>
    /// <param name="updateAnnotationDto">The data to update the annotation with.</param>
    /// <returns>The updated annotation.</returns>
    /// <response code="200">Returns the updated annotation.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the annotation is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{annotationId:long}")]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAnnotation(long annotationId, [FromBody] UpdateAnnotationDto updateAnnotationDto)
    {
        try
        {
            var updatedAnnotation = await _annotationService.UpdateAnnotationAsync(annotationId, updateAnnotationDto);

            if (updatedAnnotation == null)
            {
                return NotFound($"Annotation with ID {annotationId} not found.");
            }

            return Ok(updatedAnnotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating annotation {AnnotationId}.", annotationId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes an annotation by its ID.
    /// </summary>
    /// <param name="annotationId">The ID of the annotation to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the annotation was successfully deleted.</response>
    /// <response code="404">If the annotation is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{annotationId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAnnotation(long annotationId)
    {
        try
        {
            var result = await _annotationService.DeleteAnnotationAsync(annotationId);

            if (!result)
            {
                return NotFound($"Annotation with ID {annotationId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting annotation {AnnotationId}.", annotationId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }
}
