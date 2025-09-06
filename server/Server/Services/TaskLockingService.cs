using Microsoft.EntityFrameworkCore;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using LaberisTask = server.Models.Domain.Task;

namespace server.Services;

/// <summary>
/// Service for managing task locking to prevent concurrent access by multiple users.
/// </summary>
public class TaskLockingService : ITaskLockingService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<TaskLockingService> _logger;

    public TaskLockingService(ITaskRepository taskRepository, ILogger<TaskLockingService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> TryLockTaskAsync(int taskId, string userId, int lockDurationMinutes = 30)
    {
        _logger.LogInformation("Attempting to lock task {TaskId} for user {UserId} for {Duration} minutes", 
            taskId, userId, lockDurationMinutes);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found", taskId);
            return false;
        }

        var now = DateTime.UtcNow;

        // Check if task is already locked by another user
        if (task.LockedByUserId != null && task.LockExpiresAt.HasValue && task.LockExpiresAt > now)
        {
            if (task.LockedByUserId != userId)
            {
                _logger.LogInformation("Task {TaskId} is already locked by user {LockedUserId}, expires at {ExpiresAt}", 
                    taskId, task.LockedByUserId, task.LockExpiresAt);
                return false;
            }
            
            // User already owns the lock, extend it
            _logger.LogInformation("User {UserId} already owns lock for task {TaskId}, extending lock", userId, taskId);
        }

        // Acquire or extend the lock
        task.LockedByUserId = userId;
        task.LockedAt = now;
        task.LockExpiresAt = now.AddMinutes(lockDurationMinutes);

        _taskRepository.Update(task);
        
        try
        {
            await _taskRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully locked task {TaskId} for user {UserId}, expires at {ExpiresAt}", 
                taskId, userId, task.LockExpiresAt);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when trying to lock task {TaskId}. Lock failed.", taskId);
            return false;
        }
    }

    public async Task<bool> ReleaseLockAsync(int taskId, string userId)
    {
        _logger.LogInformation("Attempting to release lock for task {TaskId} by user {UserId}", taskId, userId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found", taskId);
            return false; // Task doesn't exist, so no lock to release.
        }

        // Check if the user owns the lock
        if (task.LockedByUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to release lock for task {TaskId} but lock is owned by {LockedUserId}", 
                userId, taskId, task.LockedByUserId);
            return false;
        }

        // Release the lock
        task.LockedByUserId = null;
        task.LockedAt = null;
        task.LockExpiresAt = null;

        _taskRepository.Update(task);
        
        try
        {
            await _taskRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully released lock for task {TaskId} by user {UserId}", taskId, userId);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when trying to release lock for task {TaskId}. Release failed.", taskId);
            return false;
        }
    }

    public async Task<bool> IsTaskLockedAsync(int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        return task.LockedByUserId != null && task.LockExpiresAt.HasValue && task.LockExpiresAt > now;
    }

    public async Task<bool> IsTaskLockedByUserAsync(int taskId, string userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        return task.LockedByUserId == userId && task.LockExpiresAt.HasValue && task.LockExpiresAt > now;
    }

    public async Task<bool> ExtendLockAsync(int taskId, string userId, int extensionMinutes = 30)
    {
        _logger.LogInformation("Attempting to extend lock for task {TaskId} by user {UserId} for {Extension} minutes", 
            taskId, userId, extensionMinutes);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found", taskId);
            return false;
        }

        var now = DateTime.UtcNow;

        // Check if the user owns an active lock
        if (task.LockedByUserId != userId || task.LockExpiresAt == null || task.LockExpiresAt <= now)
        {
            _logger.LogWarning("User {UserId} attempted to extend lock for task {TaskId} but doesn't own an active lock", 
                userId, taskId);
            return false;
        }

        // Extend the lock
        task.LockExpiresAt = now.AddMinutes(extensionMinutes);

        _taskRepository.Update(task);
        
        try
        {
            await _taskRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully extended lock for task {TaskId} by user {UserId}, new expiration: {ExpiresAt}", 
                taskId, userId, task.LockExpiresAt);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when trying to extend lock for task {TaskId}. Extension failed.", taskId);
            return false;
        }
    }

    public async Task<int> CleanupExpiredLocksAsync()
    {
        _logger.LogInformation("Starting cleanup of expired task locks");

        var now = DateTime.UtcNow;
        
        // Find all tasks with expired locks
        var expiredTasks = await _taskRepository.GetAllAsync(
            filter: t => t.LockedByUserId != null && t.LockExpiresAt.HasValue && t.LockExpiresAt <= now
        );

        if (!expiredTasks.Any())
        {
            _logger.LogInformation("No expired locks found");
            return 0;
        }

        var expiredCount = expiredTasks.Count();
        _logger.LogInformation("Found {Count} expired locks to clean up", expiredCount);

        // Clear the lock fields for expired tasks
        foreach (var task in expiredTasks)
        {
            _logger.LogDebug("Cleaning up expired lock for task {TaskId}, was locked by user {UserId}", 
                task.TaskId, task.LockedByUserId);
            
            task.LockedByUserId = null;
            task.LockedAt = null;
            task.LockExpiresAt = null;
            
            _taskRepository.Update(task);
        }

        try
        {
            await _taskRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully cleaned up {Count} expired task locks", expiredCount);
            return expiredCount;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency conflict occurred while cleaning up expired locks. Not all expired locks may have been cleaned up.");
            return 0;
        }
    }

    public async Task<IList<LaberisTask>> GetTasksLockedByUserAsync(string userId)
    {
        _logger.LogInformation("Getting all tasks locked by user {UserId}", userId);

        var now = DateTime.UtcNow;
        
        var lockedTasks = await _taskRepository.GetAllAsync(
            filter: t => t.LockedByUserId == userId && t.LockExpiresAt.HasValue && t.LockExpiresAt > now
        );

        _logger.LogInformation("Found {Count} active locks for user {UserId}", lockedTasks.Count(), userId);
        return [.. lockedTasks];
    }

    public async Task<int> ReleaseAllUserLocksAsync(string userId)
    {
        _logger.LogInformation("Releasing all locks for user {UserId}", userId);

        var userTasks = await _taskRepository.GetAllAsync(
            filter: t => t.LockedByUserId == userId
        );

        if (!userTasks.Any())
        {
            _logger.LogInformation("No locks found for user {UserId}", userId);
            return 0;
        }

        var lockCount = userTasks.Count();
        _logger.LogInformation("Found {Count} locks for user {UserId} to release", lockCount, userId);

        // Clear all locks for this user
        foreach (var task in userTasks)
        {
            _logger.LogDebug("Releasing lock for task {TaskId} from user {UserId}", task.TaskId, userId);
            
            task.LockedByUserId = null;
            task.LockedAt = null;
            task.LockExpiresAt = null;
            
            _taskRepository.Update(task);
        }

        try
        {
            await _taskRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully released {Count} locks for user {UserId}", lockCount, userId);
            return lockCount;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency conflict occurred while releasing all locks for user {UserId}.", userId);
            return 0;
        }
    }
}