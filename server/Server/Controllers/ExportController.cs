using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Models.DTOs.Export;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
[ProjectAccess]
[EnableRateLimiting("project")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExportService exportService, ILogger<ExportController> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets export metadata for a workflow stage
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">The workflow stage ID</param>
    /// <returns>Export metadata including task counts and available formats</returns>
    /// <response code="200">Returns the export metadata</response>
    /// <response code="404">If the workflow stage is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("workflow-stages/{workflowStageId:int}/metadata")]
    [ProducesResponseType(typeof(ExportMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExportMetadata(int projectId, int workflowStageId)
    {
        try
        {
            var metadata = await _exportService.GetExportMetadataAsync(projectId, workflowStageId);
            return Ok(metadata);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Workflow stage {WorkflowStageId} not found in project {ProjectId}", workflowStageId, projectId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while getting export metadata for workflow stage {WorkflowStageId} in project {ProjectId}",
                workflowStageId,
                projectId
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Exports completed tasks from a workflow stage in COCO format
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">The workflow stage ID</param>
    /// <param name="includeGroundTruth">Whether to include ground truth annotations (default: true)</param>
    /// <param name="includePredictions">Whether to include prediction annotations (default: false)</param>
    /// <returns>COCO format JSON file download</returns>
    /// <response code="200">Returns the COCO format file</response>
    /// <response code="404">If the workflow stage is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("workflow-stages/{workflowStageId:int}/coco")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportCoco(
        int projectId, 
        int workflowStageId,
        [FromQuery] bool includeGroundTruth = true,
        [FromQuery] bool includePredictions = false)
    {
        try
        {
            _logger.LogInformation("Starting COCO export for project {ProjectId}, workflow stage {WorkflowStageId}", projectId, workflowStageId);

            var cocoData = await _exportService.ExportCocoFormatAsync(projectId, workflowStageId, includeGroundTruth, includePredictions);
            
            // Get metadata for filename
            var metadata = await _exportService.GetExportMetadataAsync(projectId, workflowStageId);
            var fileName = $"{metadata.ProjectName}_{metadata.WorkflowStageName}_coco_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            
            // Sanitize filename
            fileName = SanitizeFileName(fileName);

            _logger.LogInformation("COCO export completed for project {ProjectId}, workflow stage {WorkflowStageId}. File size: {FileSize} bytes", 
                projectId, workflowStageId, cocoData.Length);

            return File(cocoData, "application/json", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Workflow stage {WorkflowStageId} not found in project {ProjectId}", workflowStageId, projectId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while exporting COCO data for workflow stage {WorkflowStageId} in project {ProjectId}",
                workflowStageId,
                projectId
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred during export. Please try again later."
            );
        }
    }

    /// <summary>
    /// Exports completed tasks from a workflow stage in COCO format (POST version for complex filtering)
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">The workflow stage ID</param>
    /// <param name="exportRequest">Export configuration options</param>
    /// <returns>COCO format JSON file download</returns>
    /// <response code="200">Returns the COCO format file</response>
    /// <response code="400">If the export request is invalid</response>
    /// <response code="404">If the workflow stage is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost("workflow-stages/{workflowStageId:int}/coco")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportCocoWithOptions(
        int projectId, 
        int workflowStageId,
        [FromBody] CocoExportRequestDto exportRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                "Starting COCO export with options for project {ProjectId}, workflow stage {WorkflowStageId}",
                projectId,
                workflowStageId
            );

            var cocoData = await _exportService.ExportCocoFormatAsync(
                projectId, 
                workflowStageId, 
                exportRequest.IncludeGroundTruth, 
                exportRequest.IncludePredictions);
            
            // Get metadata for filename
            var metadata = await _exportService.GetExportMetadataAsync(projectId, workflowStageId);
            var fileName = (
                exportRequest.FileName ??
                $"{metadata.ProjectName}_{metadata.WorkflowStageName}_coco_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
            );

            // Sanitize filename
            fileName = SanitizeFileName(fileName);
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            _logger.LogInformation(
                "COCO export with options completed for project {ProjectId}, workflow stage {WorkflowStageId}. File size: {FileSize} bytes",
                projectId,
                workflowStageId,
                cocoData.Length
            );

            return File(cocoData, "application/json", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Workflow stage {WorkflowStageId} not found in project {ProjectId}", workflowStageId, projectId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while exporting COCO data with options for workflow stage {WorkflowStageId} in project {ProjectId}",
                workflowStageId,
                projectId
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred during export. Please try again later."
            );
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Sanitizes a filename by removing invalid characters
    /// </summary>
    /// <param name="fileName">The filename to sanitize</param>
    /// <returns>A sanitized filename</returns>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        
        // Replace spaces with underscores
        sanitized = sanitized.Replace(' ', '_');

        // Limit length to 200 characters
        // TODO: Implement proper filename length handling
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }

    #endregion
}