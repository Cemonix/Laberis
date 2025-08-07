using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.Task;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
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
    /// <param name="autoAssign">Whether to automatically assign the task to the requesting user if it's unassigned (default: true).</param>
    /// <returns>The requested task.</returns>
    [HttpGet("{taskId:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(
        int taskId,
        [FromQuery] bool autoAssign = true)
    {
        try
        {
            TaskDto? task;
            
            if (autoAssign)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    task = await _taskService.GetTaskByIdWithAutoAssignAsync(taskId, userId);
                }
                else
                {
                    // Fallback to regular get if user ID is not available
                    task = await _taskService.GetTaskByIdAsync(taskId);
                }
            }
            else
            {
                task = await _taskService.GetTaskByIdAsync(taskId);
            }

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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskDto updateTaskDto)
    {
        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(taskId, updateTaskDto);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="taskId">The ID of the task to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the task was successfully deleted.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{taskId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        try
        {
            var result = await _taskService.DeleteTaskAsync(taskId);

            if (!result)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting task {TaskId}.", taskId);
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignTask(int taskId, string userId)
    {
        try
        {
            var updatedTask = await _taskService.AssignTaskAsync(taskId, userId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
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
            var updatedTask = await _taskService.AssignTaskAsync(taskId, userId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning task {TaskId} to current user {UserId}.", taskId, userId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Moves a task to a different workflow stage.
    /// </summary>
    /// <param name="taskId">The ID of the task to move.</param>
    /// <param name="workflowStageId">The ID of the workflow stage to move the task to.</param>
    /// <returns>The updated task.</returns>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/move-to-stage/{workflowStageId:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MoveTaskToStage(int taskId, int workflowStageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var updatedTask = await _taskService.MoveTaskToStageAsync(taskId, workflowStageId, userId);

            if (updatedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(updatedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while moving task {TaskId} to stage {WorkflowStageId}.", taskId, workflowStageId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Gets the count of available assets for task creation in a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>The count of available assets.</returns>
    /// <response code="200">Returns the count of available assets.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("available-assets-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableAssetsCount(int projectId)
    {
        try
        {
            var count = await _taskService.GetAvailableAssetsCountAsync(projectId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting available assets count for project {ProjectId}.", projectId);
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
    /// Marks a task as completed, unlocking the asset for subsequent workflow stages.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <returns>The completed task.</returns>
    /// <response code="200">Returns the completed task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/complete")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var completedTask = await _taskService.CompleteTaskAsync(taskId, userId);

            if (completedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(completedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Marks a task as completed and moves it to the next workflow stage if one exists.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete and move.</param>
    /// <returns>The completed task.</returns>
    /// <response code="200">Returns the completed task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/complete-and-move")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteAndMoveTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var completedTask = await _taskService.CompleteAndMoveTaskAsync(taskId, userId);

            if (completedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(completedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing and moving task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Marks a completed task as incomplete, allowing it to be worked on again.
    /// </summary>
    /// <param name="taskId">The ID of the task to mark as incomplete.</param>
    /// <returns>The uncompleted task.</returns>
    /// <response code="200">Returns the uncompleted task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="400">If the task is not in a completed state.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/incomplete")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkTaskIncomplete(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var uncompletedTask = await _taskService.MarkTaskIncompleteAsync(taskId, userId);

            if (uncompletedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(uncompletedTask);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while marking task {TaskId} as incomplete.", taskId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while marking task {TaskId} as incomplete.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Suspends a task, marking it as suspended so it can be resumed later.
    /// </summary>
    /// <param name="taskId">The ID of the task to suspend.</param>
    /// <returns>The suspended task.</returns>
    /// <response code="200">Returns the suspended task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="400">If the task cannot be suspended.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/suspend")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SuspendTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var suspendedTask = await _taskService.SuspendTaskAsync(taskId, userId);

            if (suspendedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(suspendedTask);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while suspending task {TaskId}.", taskId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while suspending task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }
    
    /// <summary>
    /// Defers a task, marking it as deferred so the user can skip it for now.
    /// </summary>
    /// <param name="taskId">The ID of the task to defer.</param>
    /// <returns>The deferred task.</returns>
    /// <response code="200">Returns the deferred task.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="400">If the task cannot be deferred.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{taskId:int}/defer")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeferTask(int taskId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var deferredTask = await _taskService.DeferTaskAsync(taskId, userId);

            if (deferredTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(deferredTask);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while deferring task {TaskId}.", taskId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deferring task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Returns a task for rework, available to reviewers (from review stages) and managers (from completion stages).
    /// This will archive the current task and move the asset back to annotation stage for rework.
    /// </summary>
    /// <param name="taskId">The ID of the task to return for rework.</param>
    /// <param name="dto">The return request containing optional reason.</param>
    /// <returns>The archived task that was returned.</returns>
    /// <response code="200">Returns the archived task.</response>
    /// <response code="400">If the task cannot be returned (wrong stage, already archived, etc.).</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the task is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{taskId:int}/return-for-rework")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReturnTaskForRework(
        int taskId,
        [FromBody] ReturnTaskForReworkDto? dto = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var returnedTask = await _taskService.ReturnTaskForReworkAsync(taskId, userId, dto?.Reason);

            if (returnedTask == null)
            {
                return NotFound($"Task with ID {taskId} not found.");
            }

            return Ok(returnedTask);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when returning task {TaskId} for rework by user {UserId}", taskId, userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while returning task {TaskId} for rework by user {UserId}", taskId, userId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
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

            var updatedTask = await _taskService.ChangeTaskStatusAsync(taskId, dto.TargetStatus, userId, dto.MoveAsset);

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
}
