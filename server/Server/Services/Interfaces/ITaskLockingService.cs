using LaberisTask = server.Models.Domain.Task;

namespace server.Services.Interfaces;

/// <summary>
/// Service for managing task locking to prevent concurrent access by multiple users.
/// </summary>
public interface ITaskLockingService
{
    /// <summary>
    /// Attempts to lock a task for a specific user.
    /// </summary>
    /// <param name="taskId">The ID of the task to lock.</param>
    /// <param name="userId">The ID of the user requesting the lock.</param>
    /// <param name="lockDurationMinutes">Duration of the lock in minutes (default: 30).</param>
    /// <returns>True if the lock was acquired, false if the task is already locked by another user.</returns>
    Task<bool> TryLockTaskAsync(int taskId, string userId, int lockDurationMinutes = 30);

    /// <summary>
    /// Releases a task lock if it's owned by the specified user.
    /// </summary>
    /// <param name="taskId">The ID of the task to unlock.</param>
    /// <param name="userId">The ID of the user requesting to unlock.</param>
    /// <returns>True if the lock was released, false if the user doesn't own the lock.</returns>
    Task<bool> ReleaseLockAsync(int taskId, string userId);

    /// <summary>
    /// Checks if a task is currently locked by any user.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <returns>True if the task is locked, false otherwise.</returns>
    Task<bool> IsTaskLockedAsync(int taskId);

    /// <summary>
    /// Checks if a task is locked by a specific user.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>True if the task is locked by the specified user, false otherwise.</returns>
    Task<bool> IsTaskLockedByUserAsync(int taskId, string userId);

    /// <summary>
    /// Extends the lock duration for a task if it's owned by the user.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="extensionMinutes">Additional minutes to extend the lock (default: 30).</param>
    /// <returns>True if the lock was extended, false if the user doesn't own the lock.</returns>
    Task<bool> ExtendLockAsync(int taskId, string userId, int extensionMinutes = 30);

    /// <summary>
    /// Cleans up expired locks from the system.
    /// </summary>
    /// <returns>Number of expired locks that were cleaned up.</returns>
    Task<int> CleanupExpiredLocksAsync();

    /// <summary>
    /// Gets all tasks currently locked by a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>List of tasks locked by the user.</returns>
    Task<IList<LaberisTask>> GetTasksLockedByUserAsync(string userId);

    /// <summary>
    /// Releases all locks held by a specific user (used when user logs out or disconnects).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Number of locks that were released.</returns>
    Task<int> ReleaseAllUserLocksAsync(string userId);
}