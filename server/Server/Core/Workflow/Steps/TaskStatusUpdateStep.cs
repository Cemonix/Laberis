using Microsoft.Extensions.Logging;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Core.Workflow.Steps;

/// <summary>
/// Pipeline step responsible for updating task status during workflow operations.
/// Implements atomic status updates with proper timestamp management and rollback capability.
/// </summary>
public class TaskStatusUpdateStep : ITaskStatusUpdateStep
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<ITaskStatusUpdateStep> _logger;
    
    // Store original status for rollback purposes
    private TaskStatus? _originalStatus;

    public TaskStatusUpdateStep(
        ITaskRepository taskRepository,
        ILogger<ITaskStatusUpdateStep> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StepName => "TaskStatusUpdateStep";

    public async Task<PipelineContext> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {StepName} for task {TaskId}", StepName, context.Task.TaskId);
        
        // Default behavior: update to COMPLETED status for forward flow
        return await UpdateStatusAsync(context, TaskStatus.COMPLETED, cancellationToken);
    }

    public async Task<PipelineContext> UpdateStatusAsync(
        PipelineContext context,
        TaskStatus targetStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Updating task {TaskId} status from {CurrentStatus} to {TargetStatus}",
            context.Task.TaskId, context.Task.Status, targetStatus);

        // Store original status for potential rollback
        _originalStatus = context.Task.Status;

        try
        {
            // Update task status using the repository
            var updatedTask = await _taskRepository.UpdateTaskStatusAsync(
                context.Task,
                targetStatus,
                context.UserId);

            _logger.LogInformation("Successfully updated task {TaskId} status to {TargetStatus}",
                context.Task.TaskId, targetStatus);

            // Create new context with updated task, preserving target stage
            var newContext = new PipelineContext(
                updatedTask,
                context.Asset,
                context.CurrentStage,
                context.UserId,
                context.Reason);
            
            // Preserve target stage if it was set
            if (context.TargetStage != null)
            {
                newContext = newContext.WithTargetStage(context.TargetStage);
            }
            
            return newContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {TaskId} status to {TargetStatus}",
                context.Task.TaskId, targetStatus);
            throw;
        }
    }

    public async Task<bool> RollbackAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_originalStatus == null)
        {
            _logger.LogWarning("No original status stored for rollback of task {TaskId}", context.Task.TaskId);
            return false;
        }

        _logger.LogInformation("Rolling back task {TaskId} status from {CurrentStatus} to {OriginalStatus}",
            context.Task.TaskId, context.Task.Status, _originalStatus);

        try
        {
            await _taskRepository.UpdateTaskStatusAsync(
                context.Task,
                _originalStatus.Value,
                context.UserId);

            _logger.LogInformation("Successfully rolled back task {TaskId} status to {OriginalStatus}",
                context.Task.TaskId, _originalStatus);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback task {TaskId} status to {OriginalStatus}",
                context.Task.TaskId, _originalStatus);
            return false;
        }
    }
}