using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

/// <summary>
/// Controller for managing task locks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskLockController : ControllerBase
{
    private readonly ITaskLockingService _taskLockingService;
    private readonly ILogger<TaskLockController> _logger;

    public TaskLockController(ITaskLockingService taskLockingService, ILogger<TaskLockController> logger)
    {
        _taskLockingService = taskLockingService ?? throw new ArgumentNullException(nameof(taskLockingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to lock a task for the current user.
    /// </summary>
    /// <param name="taskId">The ID of the task to lock.</param>
    /// <param name="lockDurationMinutes">Duration of the lock in minutes (default: 30).</param>
    /// <returns>Success status of the lock attempt.</returns>
    [HttpPost("{taskId}/lock")]
    public async Task<IActionResult> LockTaskAsync(int taskId, [FromQuery] int lockDurationMinutes = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in claims");
        }

        _logger.LogInformation("User {UserId} attempting to lock task {TaskId} for {Duration} minutes", userId, taskId, lockDurationMinutes);

        var success = await _taskLockingService.TryLockTaskAsync(taskId, userId, lockDurationMinutes);

        if (success)
        {
            _logger.LogInformation("Successfully locked task {TaskId} for user {UserId}", taskId, userId);
            return Ok(new { success = true, message = "Task locked successfully" });
        }
        else
        {
            _logger.LogInformation("Failed to lock task {TaskId} for user {UserId} - already locked by another user", taskId, userId);
            return BadRequest(new { success = false, message = "Task is already locked by another user" });
        }
    }

    /// <summary>
    /// Releases a task lock owned by the current user.
    /// </summary>
    /// <param name="taskId">The ID of the task to unlock.</param>
    /// <returns>Success status of the unlock attempt.</returns>
    [HttpPost("{taskId}/unlock")]
    public async Task<IActionResult> UnlockTaskAsync(int taskId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in claims");
        }

        _logger.LogInformation("User {UserId} attempting to unlock task {TaskId}", userId, taskId);

        var success = await _taskLockingService.ReleaseLockAsync(taskId, userId);

        if (success)
        {
            _logger.LogInformation("Successfully unlocked task {TaskId} for user {UserId}", taskId, userId);
            return Ok(new { success = true, message = "Task unlocked successfully" });
        }
        else
        {
            _logger.LogInformation("Failed to unlock task {TaskId} for user {UserId} - user doesn't own the lock", taskId, userId);
            return BadRequest(new { success = false, message = "You don't own the lock for this task" });
        }
    }

    /// <summary>
    /// Extends the lock duration for a task owned by the current user.
    /// </summary>
    /// <param name="taskId">The ID of the task to extend the lock for.</param>
    /// <param name="extensionMinutes">Additional minutes to extend the lock (default: 30).</param>
    /// <returns>Success status of the lock extension.</returns>
    [HttpPost("{taskId}/extend-lock")]
    public async Task<IActionResult> ExtendLockAsync(int taskId, [FromQuery] int extensionMinutes = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in claims");
        }

        _logger.LogInformation("User {UserId} attempting to extend lock for task {TaskId} by {Extension} minutes", userId, taskId, extensionMinutes);

        var success = await _taskLockingService.ExtendLockAsync(taskId, userId, extensionMinutes);

        if (success)
        {
            _logger.LogInformation("Successfully extended lock for task {TaskId} for user {UserId}", taskId, userId);
            return Ok(new { success = true, message = "Task lock extended successfully" });
        }
        else
        {
            _logger.LogInformation("Failed to extend lock for task {TaskId} for user {UserId} - user doesn't own an active lock", taskId, userId);
            return BadRequest(new { success = false, message = "You don't own an active lock for this task" });
        }
    }

    /// <summary>
    /// Checks if a task is currently locked.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <returns>Lock status of the task.</returns>
    [HttpGet("{taskId}/is-locked")]
    public async Task<IActionResult> IsTaskLockedAsync(int taskId)
    {
        var isLocked = await _taskLockingService.IsTaskLockedAsync(taskId);
        return Ok(new { isLocked });
    }

    /// <summary>
    /// Gets all tasks currently locked by the current user.
    /// </summary>
    /// <returns>List of tasks locked by the user.</returns>
    [HttpGet("my-locks")]
    public async Task<IActionResult> GetMyLockedTasksAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in claims");
        }

        var lockedTasks = await _taskLockingService.GetTasksLockedByUserAsync(userId);
        return Ok(lockedTasks);
    }

    /// <summary>
    /// Releases all locks held by the current user (useful for logout/disconnect scenarios).
    /// </summary>
    /// <returns>Number of locks that were released.</returns>
    [HttpPost("release-all")]
    public async Task<IActionResult> ReleaseAllLocksAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in claims");
        }

        _logger.LogInformation("User {UserId} releasing all locks", userId);

        var releasedCount = await _taskLockingService.ReleaseAllUserLocksAsync(userId);

        _logger.LogInformation("Released {Count} locks for user {UserId}", releasedCount, userId);
        return Ok(new { releasedCount, message = $"Released {releasedCount} locks" });
    }
}