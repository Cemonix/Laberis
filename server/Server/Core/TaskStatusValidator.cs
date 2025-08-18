using server.Models.Domain.Enums;
using server.Services.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Services;

public class TaskStatusValidator : ITaskStatusValidator
{
    private readonly ILogger<TaskStatusValidator> _logger;

    public TaskStatusValidator(ILogger<TaskStatusValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ValidateStatusTransition(
        LaberisTask task, 
        TaskStatus currentStatus, 
        TaskStatus targetStatus, 
        string userId)
    {
        _logger.LogDebug("Validating status transition for task {TaskId}: {CurrentStatus} → {TargetStatus}", 
            task.TaskId, currentStatus, targetStatus);

        bool result = targetStatus switch
        {
            TaskStatus.NOT_STARTED => false,
            TaskStatus.READY_FOR_ANNOTATION => false,
            TaskStatus.READY_FOR_REVIEW => false,
            TaskStatus.READY_FOR_COMPLETION => false,
            TaskStatus.IN_PROGRESS => ValidateInProgressTransition(currentStatus),
            TaskStatus.SUSPENDED => ValidateSuspendedTransition(currentStatus),
            TaskStatus.DEFERRED => ValidateDeferredTransition(currentStatus),
            TaskStatus.COMPLETED => ValidateCompletedTransition(currentStatus),
            TaskStatus.VETOED => ValidateVetoedTransition(currentStatus),
            TaskStatus.CHANGES_REQUIRED => ValidateChangesRequiredTransition(currentStatus),
            TaskStatus.ARCHIVED => ValidateArchivedTransition(currentStatus),
            _ => false
        };

        return result;
    }

    private static bool ValidateInProgressTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.READY_FOR_ANNOTATION or TaskStatus.READY_FOR_REVIEW or TaskStatus.READY_FOR_COMPLETION => true,
            TaskStatus.SUSPENDED => true, // Unsuspending
            TaskStatus.DEFERRED => true, // Undefer
            TaskStatus.NOT_STARTED => true,
            TaskStatus.CHANGES_REQUIRED => true, // Allow resuming tasks that require changes
            _ => false
        };
    }

    private static bool ValidateSuspendedTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS
            or TaskStatus.READY_FOR_ANNOTATION
            or TaskStatus.READY_FOR_REVIEW
            or TaskStatus.READY_FOR_COMPLETION
            or TaskStatus.NOT_STARTED
            or TaskStatus.CHANGES_REQUIRED => true, // Allow suspending tasks that require changes
            _ => false
        };
    }

    private static bool ValidateDeferredTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS
            or TaskStatus.READY_FOR_ANNOTATION
            or TaskStatus.READY_FOR_REVIEW
            or TaskStatus.READY_FOR_COMPLETION
            or TaskStatus.NOT_STARTED
            or TaskStatus.CHANGES_REQUIRED => true, // Allow deferring tasks that require changes
            _ => false
        };
    }

    private static bool ValidateCompletedTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS => true,
            TaskStatus.CHANGES_REQUIRED => true, // Allow completing tasks that require changes (after fixing them)
            _ => false
        };
    }

    private static bool ValidateVetoedTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS => true,
            _ => false
        };
    }

    private static bool ValidateChangesRequiredTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.COMPLETED => true,
            _ => false
        };
    }

    private static bool ValidateArchivedTransition(TaskStatus currentStatus)
    {
        return currentStatus switch
        {
            TaskStatus.COMPLETED => true,
            _ => false
        };
    }
}