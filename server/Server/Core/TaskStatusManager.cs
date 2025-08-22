using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Core;

/// <summary>
/// Core business logic for managing task status changes.
/// Centralizes status change operations to ensure consistency across the application.
/// </summary>
public static class TaskStatusManager
{
    /// <summary>
    /// Applies a status change to a task with appropriate timestamp updates.
    /// This method modifies the task object in-place.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="targetStatus">The new status to apply.</param>
    /// <param name="userId">The ID of the user making the change.</param>
    public static void ApplyStatusChange(LaberisTask task, TaskStatus targetStatus, string userId)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var now = DateTime.UtcNow;

        task.Status = targetStatus;
        task.UpdatedAt = now;
        task.LastWorkedOnByUserId = userId;

        // Update status-specific timestamps
        switch (targetStatus)
        {
            case TaskStatus.SUSPENDED:
                task.SuspendedAt = now;
                break;
            case TaskStatus.DEFERRED:
                task.DeferredAt = now;
                break;
            case TaskStatus.COMPLETED:
                task.CompletedAt = now;
                break;
            case TaskStatus.ARCHIVED:
                task.ArchivedAt = now;
                task.CompletedAt = now; // Archived tasks are also completed
                break;
            case TaskStatus.VETOED:
                task.VetoedAt = now;
                break;
            case TaskStatus.CHANGES_REQUIRED:
                task.ChangesRequiredAt = now;
                break;
            default:
                // For other statuses (IN_PROGRESS, NOT_STARTED, etc.), no additional timestamps needed
                break;
        }
    }

}