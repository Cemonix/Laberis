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

    public (bool IsValid, string ErrorMessage) ValidateStatusTransition(
        LaberisTask task, 
        TaskStatus currentStatus, 
        TaskStatus targetStatus, 
        string userId)
    {
        _logger.LogDebug("Validating status transition for task {TaskId}: {CurrentStatus} → {TargetStatus}", 
            task.TaskId, currentStatus, targetStatus);

        var stageType = task.CurrentWorkflowStage?.StageType;
        
        // TODO: Add role-based validation (manager vs regular user)
        // For now, we'll implement the basic transition rules
        
        var result = targetStatus switch
        {
            TaskStatus.IN_PROGRESS => ValidateInProgressTransition(currentStatus, stageType),
            TaskStatus.COMPLETED => ValidateCompletedTransition(currentStatus, stageType),
            TaskStatus.SUSPENDED => ValidateSuspendedTransition(currentStatus, stageType),
            TaskStatus.DEFERRED => ValidateDeferredTransition(currentStatus, stageType),
            TaskStatus.ARCHIVED => ValidateArchivedTransition(currentStatus, stageType),
            TaskStatus.READY_FOR_ANNOTATION => ValidateReadyForAnnotationTransition(currentStatus, stageType),
            TaskStatus.READY_FOR_REVIEW => ValidateReadyForReviewTransition(currentStatus, stageType),
            TaskStatus.READY_FOR_COMPLETION => ValidateReadyForCompletionTransition(currentStatus, stageType),
            TaskStatus.NOT_STARTED => ValidateNotStartedTransition(currentStatus, stageType),
            _ => (false, $"Unknown target status: {targetStatus}")
        };

        if (!result.Item1)
        {
            _logger.LogWarning("Invalid status transition for task {TaskId}: {ErrorMessage}", 
                task.TaskId, result.Item2);
        }

        return result;
    }

    private static (bool IsValid, string ErrorMessage) ValidateInProgressTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return currentStatus switch
        {
            TaskStatus.READY_FOR_ANNOTATION or TaskStatus.READY_FOR_REVIEW or TaskStatus.READY_FOR_COMPLETION => (true, string.Empty),
            TaskStatus.SUSPENDED => (true, string.Empty), // Unsuspending
            TaskStatus.DEFERRED => (true, string.Empty), // Undefer
            TaskStatus.NOT_STARTED => (true, string.Empty),
            _ => (false, $"Cannot change status from {currentStatus} to IN_PROGRESS")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateCompletedTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS => (true, string.Empty),
            _ => (false, $"Cannot complete task from status {currentStatus}")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateSuspendedTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS or TaskStatus.READY_FOR_ANNOTATION or TaskStatus.READY_FOR_REVIEW or TaskStatus.READY_FOR_COMPLETION => (true, string.Empty),
            TaskStatus.COMPLETED => (false, "Cannot suspend a completed task"),
            TaskStatus.ARCHIVED => (false, "Cannot suspend an archived task"),
            _ => (false, $"Cannot suspend task from status {currentStatus}")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateDeferredTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return currentStatus switch
        {
            TaskStatus.IN_PROGRESS or TaskStatus.READY_FOR_ANNOTATION or TaskStatus.READY_FOR_REVIEW or TaskStatus.READY_FOR_COMPLETION => (true, string.Empty),
            TaskStatus.SUSPENDED => (false, "Cannot defer a suspended task - please unsuspend first"),
            TaskStatus.COMPLETED => (false, "Cannot defer a completed task"),
            TaskStatus.ARCHIVED => (false, "Cannot defer an archived task"),
            _ => (false, $"Cannot defer task from status {currentStatus}")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateArchivedTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return currentStatus switch
        {
            TaskStatus.COMPLETED => (true, string.Empty),
            _ => (false, $"Cannot archive task from status {currentStatus}")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateReadyForAnnotationTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        // This is mainly for manager marking task incomplete in completion stage
        return stageType switch
        {
            WorkflowStageType.COMPLETION when currentStatus == TaskStatus.COMPLETED => (true, string.Empty),
            _ => (false, $"Cannot change to READY_FOR_ANNOTATION from {currentStatus} in {stageType} stage")
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateReadyForReviewTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        // Typically set when task moves from annotation to review stage
        return (false, "READY_FOR_REVIEW status is typically set automatically during workflow progression");
    }

    private static (bool IsValid, string ErrorMessage) ValidateReadyForCompletionTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        // Typically set when task moves from review to completion stage
        return (false, "READY_FOR_COMPLETION status is typically set automatically during workflow progression");
    }

    private static (bool IsValid, string ErrorMessage) ValidateNotStartedTransition(TaskStatus currentStatus, WorkflowStageType? stageType)
    {
        return (false, "NOT_STARTED is typically only set during task creation");
    }
}