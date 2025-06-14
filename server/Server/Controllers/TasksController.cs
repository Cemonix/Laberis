using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.Task;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
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
    /// Gets a specific task by its unique ID.
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
            var task = await _taskService.GetTaskByIdAsync(taskId);

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
}
