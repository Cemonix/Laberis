using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Models.DTOs.Task;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
[ProjectAccess]
[EnableRateLimiting("project")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskWorkflowService _taskWorkflowService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ITaskWorkflowService taskWorkflowService, ILogger<TasksController> logger)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _taskWorkflowService = taskWorkflowService ?? throw new ArgumentNullException(nameof(taskWorkflowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all tasks for a specific project with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="projectId">The ID of the project to get tasks for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "priority", "assigned_to_user_id", "current_workflow_stage_id", "is_completed").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "priority", "due_date", "created_at", "completed_at").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of task DTOs.</returns>
    /// <response code="200">Returns the list of task DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTasksForProject(
        int projectId,
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
            var tasks = await _taskService.GetTasksForProjectAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching tasks for project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all tasks assigned to the current user with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="filterOn">The field to filter on (e.g., "priority", "project_id", "current_workflow_stage_id", "is_completed").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "priority", "due_date", "created_at", "completed_at").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of task DTOs.</returns>
    /// <response code="200">Returns the list of task DTOs.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var tasks = await _taskService.GetTasksForUserAsync(
                userId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching tasks for current user {UserId}.", userId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific task by its unique ID, with optional auto-assignment to the requesting user.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <returns>The requested task.</returns>
    [HttpGet("{taskId:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(int taskId)
    {
        try
        {
            TaskDto? task = await _taskService.GetTaskByIdAsync(taskId);
            
            if (task == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching task {TaskId}.", taskId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new task for a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the task for.</param>
    /// <param name="createTaskDto">The task creation data.</param>
    /// <returns>The newly created task.</returns>
    /// <response code="201">Returns the newly created task and its location.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [Authorize(Policy = "RequireManagerRole")]  // Only managers can manually create tasks
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTask(int projectId, [FromBody] CreateTaskDto createTaskDto)
    {
        try
        {
            var newTask = await _taskService.CreateTaskAsync(projectId, createTaskDto);
            return CreatedAtAction(nameof(GetTaskById),
                new { projectId, taskId = newTask.Id }, newTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating task for project {ProjectId}.", projectId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="taskId">The ID of the task to update.</param>
    /// <param name="updateTaskDto">The data to update the task with.</param>
    /// <returns>The updated task.</returns>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskDto updateTaskDto)
    {
        var updatingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(taskId, updateTaskDto, updatingUserId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized task update attempt by user {UpdatingUserId} for task {TaskId}.", updatingUserId, taskId);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Assigns a task to a user.
    /// </summary>
    /// <param name="taskId">The ID of the task to assign.</param>
    /// <param name="userId">The ID of the user to assign the task to.</param>
    /// <returns>The updated task.</returns>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/assign/{userId}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignTask(int taskId, string userId)
    {
        var assigningUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(assigningUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var updatedTask = await _taskService.AssignTaskAsync(taskId, userId, assigningUserId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized task assignment attempt by user {AssigningUserId} for task {TaskId} to user {UserId}.", assigningUserId, taskId, userId);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning task {TaskId} to user {UserId}.", taskId, userId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Assigns a task to the current authenticated user.
    /// </summary>
    /// <param name="taskId">The ID of the task to assign to current user.</param>
    /// <returns>The updated task.</returns>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/assign-to-me")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignTaskToCurrentUser(int taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var updatedTask = await _taskService.AssignTaskAsync(taskId, userId, userId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized self-assignment attempt by user {UserId} for task {TaskId}.", userId, taskId);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning task {TaskId} to current user {UserId}.", taskId, userId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Gets all tasks for a specific workflow stage, properly filtered by the stage's input data source.
    /// This ensures only tasks for assets that belong to the stage's assigned data source are returned.
    /// </summary>
    /// <param name="projectId">The ID of the project to get tasks for.</param>
    /// <param name="stageId">The ID of the workflow stage to get tasks for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "priority", "assigned_to_user_id", "is_completed").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "priority", "due_date", "created_at", "completed_at").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of task DTOs for the workflow stage.</returns>
    /// <response code="200">Returns the list of task DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("stage/{stageId:int}")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTasksForWorkflowStage(
        int projectId,
        int stageId,
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
            var tasks = await _taskService.GetTasksForWorkflowStageAsync(
                projectId, stageId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching tasks for workflow stage {StageId} in project {ProjectId}.", stageId, projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Changes the status of a task using the unified status change system with workflow-aware validation.
    /// This endpoint handles all task status transitions according to workflow stage rules.
    /// </summary>
    /// <param name="taskId">The ID of the task to change status for.</param>
    /// <param name="dto">The status change request containing target status and options.</param>
    /// <returns>The updated task with the new status.</returns>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="400">If the status transition is invalid or request data is malformed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeTaskStatus(int taskId, [FromBody] ChangeTaskStatusDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var updatedTask = await _taskService.ChangeTaskStatusAsync(taskId, dto.TargetStatus, userId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while changing status of task {TaskId} to {TargetStatus}.", taskId, dto.TargetStatus);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing status of task {TaskId} to {TargetStatus}.", taskId, dto.TargetStatus);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Completes a task using the workflow pipeline system.
    /// This triggers the complete workflow progression including asset transfer and next stage task creation.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="dto">The task completion request containing optional notes.</param>
    /// <returns>The pipeline result with updated task information.</returns>
    /// <response code="200">Returns the pipeline result with updated task.</response>
    /// <response code="400">If the completion operation is invalid or request data is malformed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have permission to complete this task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/complete")]
    [ProducesResponseType(typeof(PipelineResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteTask(int taskId, [FromBody] CompleteTaskDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var result = await _taskWorkflowService.CompleteTaskAsync(taskId, userId);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                else if (result.ErrorMessage?.Contains("permission") == true)
                {
                    return StatusCode(403, result.ErrorMessage);
                }
                else
                {
                    return BadRequest(result.ErrorMessage);
                }
            }

            // Map PipelineResult to PipelineResultDto
            var responseDto = new PipelineResultDto
            {
                IsSuccess = result.IsSuccess,
                Details = result.Details,
                UpdatedTask = result.UpdatedTask != null ? MapTaskToDto(result.UpdatedTask) : null
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Vetoes a task using the workflow pipeline system.
    /// This triggers the veto workflow progression including asset transfer back to annotation and task updates.
    /// </summary>
    /// <param name="taskId">The ID of the task to veto.</param>
    /// <param name="dto">The task veto request containing the reason for veto.</param>
    /// <returns>The pipeline result with updated task information.</returns>
    /// <response code="200">Returns the pipeline result with updated task.</response>
    /// <response code="400">If the veto operation is invalid or request data is malformed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have permission to veto this task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/veto")]
    [ProducesResponseType(typeof(PipelineResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VetoTask(int taskId, [FromBody] VetoTaskDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var result = await _taskWorkflowService.VetoTaskAsync(taskId, userId, dto.Reason);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                else if (result.ErrorMessage?.Contains("permission") == true)
                {
                    return StatusCode(403, result.ErrorMessage);
                }
                else
                {
                    return BadRequest(result.ErrorMessage);
                }
            }

            // Map PipelineResult to PipelineResultDto
            var responseDto = new PipelineResultDto
            {
                IsSuccess = result.IsSuccess,
                Details = result.Details,
                UpdatedTask = result.UpdatedTask != null ? MapTaskToDto(result.UpdatedTask) : null
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while vetoing task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Checks if the current user can complete the specified task.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <returns>Boolean indicating if the task can be completed.</returns>
    /// <response code="200">Returns true if the task can be completed, false otherwise.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{taskId:int}/can-complete")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanCompleteTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var canComplete = await _taskWorkflowService.CanCompleteTaskAsync(taskId, userId);
            return Ok(canComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking completion permissions for task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Checks if the current user can veto the specified task.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <returns>Boolean indicating if the task can be vetoed.</returns>
    /// <response code="200">Returns true if the task can be vetoed, false otherwise.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{taskId:int}/can-veto")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanVetoTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var canVeto = await _taskWorkflowService.CanVetoTaskAsync(taskId, userId);
            return Ok(canVeto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking veto permissions for task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a domain Task to TaskDto
    /// </summary>
    private static TaskDto MapTaskToDto(Models.Domain.Task task)
    {
        return new TaskDto
        {
            Id = task.TaskId,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            ArchivedAt = task.ArchivedAt,
            SuspendedAt = task.SuspendedAt,
            DeferredAt = task.DeferredAt,
            VetoedAt = task.VetoedAt,
            ChangesRequiredAt = task.ChangesRequiredAt,
            WorkingTimeMs = task.WorkingTimeMs,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            AssetId = task.AssetId,
            ProjectId = task.ProjectId,
            WorkflowId = task.WorkflowId,
            WorkflowStageId = task.WorkflowStageId,
            AssignedToEmail = task.AssignedToUser?.Email,
            LastWorkedOnByEmail = task.LastWorkedOnByUser?.Email
        };
    }

    #endregion
}
