using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Models.DTOs.Task;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

/// <summary>
/// Controller for task navigation operations
/// </summary>
[Route("api/projects/{projectId:int}/tasks/navigation")]
[ApiController]
[Authorize]
[ProjectAccess]
[EnableRateLimiting("project")]
public class TaskNavigationController : ControllerBase
{
    private readonly ITaskNavigationService _taskNavigationService;
    private readonly ILogger<TaskNavigationController> _logger;

    public TaskNavigationController(
        ITaskNavigationService taskNavigationService, 
        ILogger<TaskNavigationController> logger)
    {
        _taskNavigationService = taskNavigationService ?? throw new ArgumentNullException(nameof(taskNavigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the next available task for the current user
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Navigation information for the next task</returns>
    /// <response code="200">Returns the navigation information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have access to the project</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("next")]
    [ProducesResponseType(typeof(TaskNavigationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNextTask(
        int projectId,
        [FromQuery] int currentTaskId,
        [FromQuery] int? workflowStageId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            _logger.LogInformation("Getting next task for user {UserId} in project {ProjectId}, current task {CurrentTaskId}, stage {WorkflowStageId}", 
                userId, projectId, currentTaskId, workflowStageId);

            var result = await _taskNavigationService.GetNextTaskAsync(userId, currentTaskId, workflowStageId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next task for user {UserId} in project {ProjectId}", userId, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while getting next task.");
        }
    }

    /// <summary>
    /// Gets the previous available task for the current user
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Navigation information for the previous task</returns>
    /// <response code="200">Returns the navigation information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have access to the project</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("previous")]
    [ProducesResponseType(typeof(TaskNavigationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPreviousTask(
        int projectId,
        [FromQuery] int currentTaskId,
        [FromQuery] int? workflowStageId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            _logger.LogInformation("Getting previous task for user {UserId} in project {ProjectId}, current task {CurrentTaskId}, stage {WorkflowStageId}", 
                userId, projectId, currentTaskId, workflowStageId);

            var result = await _taskNavigationService.GetPreviousTaskAsync(userId, currentTaskId, workflowStageId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous task for user {UserId} in project {ProjectId}", userId, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while getting previous task.");
        }
    }

    /// <summary>
    /// Gets the navigation context for the current task
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Navigation context information</returns>
    /// <response code="200">Returns the navigation context</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have access to the project</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("context")]
    [ProducesResponseType(typeof(TaskNavigationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNavigationContext(
        int projectId,
        [FromQuery] int currentTaskId,
        [FromQuery] int? workflowStageId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            _logger.LogInformation("Getting navigation context for user {UserId} in project {ProjectId}, current task {CurrentTaskId}, stage {WorkflowStageId}", 
                userId, projectId, currentTaskId, workflowStageId);

            var result = await _taskNavigationService.GetNavigationContextAsync(userId, currentTaskId, workflowStageId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navigation context for user {UserId} in project {ProjectId}", userId, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while getting navigation context.");
        }
    }
}