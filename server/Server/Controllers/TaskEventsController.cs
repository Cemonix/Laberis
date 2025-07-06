using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.TaskEvent;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/tasks/{taskId:int}/events")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class TaskEventsController : ControllerBase
{
    private readonly ITaskEventService _taskEventService;
    private readonly ILogger<TaskEventsController> _logger;

    public TaskEventsController(ITaskEventService taskEventService, ILogger<TaskEventsController> logger)
    {
        _taskEventService = taskEventService ?? throw new ArgumentNullException(nameof(taskEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all events for a specific task with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="taskId">The ID of the task to get events for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "event_type", "user_id").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "event_type").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of task event DTOs.</returns>
    /// <response code="200">Returns the list of task event DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTaskEvents(
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
            var events = await _taskEventService.GetTaskEventsAsync(
                taskId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching events for task {TaskId}.", taskId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific task event by its unique ID.
    /// </summary>
    /// <param name="taskId">The ID of the task the event belongs to.</param>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>The requested task event.</returns>
    [HttpGet("{eventId:long}")]
    [ProducesResponseType(typeof(TaskEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskEventById(int taskId, long eventId)
    {
        try
        {
            var taskEvent = await _taskEventService.GetTaskEventByIdAsync(eventId);

            if (taskEvent == null)
            {
                return NotFound($"Task event with ID {eventId} not found.");
            }

            // Verify the event belongs to the specified task
            if (taskEvent.TaskId != taskId)
            {
                return NotFound($"Task event with ID {eventId} does not belong to task {taskId}.");
            }

            return Ok(taskEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching task event {EventId}.", eventId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new task event.
    /// </summary>
    /// <param name="taskId">The ID of the task to create the event for.</param>
    /// <param name="createTaskEventDto">The task event creation data.</param>
    /// <returns>The newly created task event.</returns>
    /// <response code="201">Returns the newly created task event and its location.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTaskEvent(int taskId, [FromBody] CreateTaskEventDto createTaskEventDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the current user ID from the token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var newTaskEvent = await _taskEventService.LogTaskEventAsync(createTaskEventDto, userIdClaim);
            return CreatedAtAction(nameof(GetTaskEventById), 
                new { taskId = taskId, eventId = newTaskEvent.Id }, newTaskEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating event for task {TaskId}.", taskId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }
}
