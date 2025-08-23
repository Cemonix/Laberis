using Microsoft.Extensions.Logging;
using server.Core.Workflow.Interfaces;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Core.Workflow.Steps;

/// <summary>
/// Pipeline step responsible for managing task creation and updates during workflow operations.
/// Implements atomic task management with proper rollback capability for pipeline consistency.
/// </summary>
public class TaskManagementStep : ITaskManagementStep
{
    private readonly ITaskRepository _taskRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IWorkflowStageResolver _workflowStageResolver;
    private readonly ILogger<ITaskManagementStep> _logger;
    
    // Store task operation info for rollback purposes
    private enum TaskOperation { None, Created, Updated }
    private TaskOperation _lastOperation = TaskOperation.None;
    private int? _createdTaskId;

    public TaskManagementStep(
        ITaskRepository taskRepository,
        IWorkflowStageRepository workflowStageRepository,
        IWorkflowStageResolver workflowStageResolver,
        ILogger<ITaskManagementStep> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _workflowStageResolver = workflowStageResolver ?? throw new ArgumentNullException(nameof(workflowStageResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StepName => "TaskManagementStep";

    public async Task<PipelineContext> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {StepName} for asset {AssetId}", StepName, context.Asset.AssetId);
        
        // Default behavior: create or update task for target stage
        return await CreateOrUpdateTaskForTargetStageAsync(context, cancellationToken);
    }

    public async Task<PipelineContext> CreateOrUpdateTaskForTargetStageAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TargetStage == null)
        {
            throw new InvalidOperationException("Target stage is required for task management");
        }

        _logger.LogInformation("Creating or updating task for asset {AssetId} in target stage {StageId}",
            context.Asset.AssetId, context.TargetStage.WorkflowStageId);

        try
        {
            // Check if a task already exists for this asset and target stage
            var existingTask = await _taskRepository.FindByAssetAndStageAsync(
                context.Asset.AssetId, 
                context.TargetStage.WorkflowStageId);

            if (existingTask == null)
            {
                // Create a new task
                var newTask = new LaberisTask
                {
                    AssetId = context.Asset.AssetId,
                    WorkflowStageId = context.TargetStage.WorkflowStageId,
                    ProjectId = context.Asset.ProjectId,
                    WorkflowId = context.TargetStage.WorkflowId,
                    Status = TaskStatus.NOT_STARTED,
                    AssignedToUserId = null, // Will be assigned later based on workflow stage assignments
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _taskRepository.AddAsync(newTask);
                await _taskRepository.SaveChangesAsync();
                _lastOperation = TaskOperation.Created;
                _createdTaskId = newTask.TaskId;

                _logger.LogInformation("Created new task {TaskId} for asset {AssetId} in stage {StageId}",
                    newTask.TaskId, context.Asset.AssetId, context.TargetStage.WorkflowStageId);
            }
            else
            {
                // Update existing task to appropriate ready status based on target stage type
                var readyStatus = GetReadyStatusForStageType(context.TargetStage.StageType);
                await _taskRepository.UpdateTaskStatusAsync(existingTask, readyStatus, context.UserId);
                _lastOperation = TaskOperation.Updated;

                _logger.LogInformation("Updated existing task {TaskId} to {Status} status for asset {AssetId}",
                    existingTask.TaskId, readyStatus, context.Asset.AssetId);
            }

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update task for asset {AssetId} in target stage {StageId}",
                context.Asset.AssetId, context.TargetStage?.WorkflowStageId);
            throw;
        }
    }

    public async Task<PipelineContext> UpdateAnnotationTaskForChangesAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogInformation("Updating annotation task for changes for asset {AssetId} after veto",
            context.Asset.AssetId);

        try
        {
            // Find the first annotation stage for this workflow (consistent with TaskVetoPipeline)
            var annotationStage = await _workflowStageResolver.GetFirstAnnotationStageAsync(context.CurrentStage.WorkflowId, cancellationToken);

            if (annotationStage == null)
            {
                throw new InvalidOperationException(
                    $"First annotation stage not found for workflow {context.CurrentStage.WorkflowId}");
            }

            // Find the annotation task for this asset
            var annotationTask = await _taskRepository.FindByAssetAndStageAsync(
                context.Asset.AssetId, 
                annotationStage.WorkflowStageId);

            if (annotationTask == null)
            {
                // No annotation task exists - this is the imported asset scenario
                // Create a new task with CHANGES_REQUIRED status
                var newTask = new LaberisTask
                {
                    AssetId = context.Asset.AssetId,
                    WorkflowStageId = annotationStage.WorkflowStageId,
                    ProjectId = context.Asset.ProjectId,
                    WorkflowId = annotationStage.WorkflowId,
                    Status = TaskStatus.CHANGES_REQUIRED,
                    AssignedToUserId = null, // Will be assigned later based on workflow stage assignments
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _taskRepository.AddAsync(newTask);
                await _taskRepository.SaveChangesAsync();
                _lastOperation = TaskOperation.Created;
                _createdTaskId = newTask.TaskId;

                _logger.LogInformation("Created new annotation task {TaskId} with CHANGES_REQUIRED status for asset {AssetId} (imported asset scenario)",
                    newTask.TaskId, context.Asset.AssetId);
            }
            else
            {
                // Annotation task exists - update it to CHANGES_REQUIRED status
                await _taskRepository.UpdateTaskStatusAsync(
                    annotationTask, 
                    TaskStatus.CHANGES_REQUIRED, 
                    context.UserId);

                _logger.LogInformation("Updated existing annotation task {TaskId} to CHANGES_REQUIRED status for asset {AssetId}",
                    annotationTask.TaskId, context.Asset.AssetId);
            }

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update annotation task for changes for asset {AssetId}",
                context.Asset.AssetId);
            throw;
        }
    }

    public async Task<bool> ValidateDataIntegrityAsync(
        PipelineContext context,
        LaberisTask? targetTask,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Validating data integrity for asset {AssetId}", context.Asset.AssetId);

        try
        {
            // Get all tasks for this asset to check for conflicts
            var allTasks = await _taskRepository.GetTasksByAssetIdAsync(context.Asset.AssetId);
            var taskList = allTasks.ToList();

            // Check for conflicting IN_PROGRESS tasks
            var inProgressTasks = taskList.Where(t => t.Status == TaskStatus.IN_PROGRESS).ToList();
            
            if (inProgressTasks.Count > 1)
            {
                _logger.LogWarning("Data integrity violation: Multiple IN_PROGRESS tasks found for asset {AssetId}. Tasks: {TaskIds}",
                    context.Asset.AssetId, string.Join(", ", inProgressTasks.Select(t => t.TaskId)));
                return false;
            }

            // Additional integrity checks can be added here
            // For example: ensuring workflow stage progression is logical

            _logger.LogInformation("Data integrity validation passed for asset {AssetId}", context.Asset.AssetId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate data integrity for asset {AssetId}", context.Asset.AssetId);
            return false;
        }
    }

    public async Task<bool> RollbackAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Rolling back task management operations for asset {AssetId}", context.Asset.AssetId);

        try
        {
            if (_lastOperation == TaskOperation.Created && _createdTaskId.HasValue)
            {
                // Delete the task that was created
                var taskToDelete = await _taskRepository.GetByIdAsync(_createdTaskId.Value);
                if (taskToDelete != null)
                {
                    _taskRepository.Remove(taskToDelete);
                    var saveResult = await _taskRepository.SaveChangesAsync();
                    var deleteResult = saveResult > 0;
                
                    if (!deleteResult)
                    {
                        _logger.LogError("Failed to delete created task {TaskId} during rollback", _createdTaskId.Value);
                        return false;
                    }

                    _logger.LogInformation("Successfully deleted created task {TaskId} during rollback", _createdTaskId.Value);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Task {TaskId} not found for rollback deletion", _createdTaskId.Value);
                    return false;
                }
            }
            else if (_lastOperation == TaskOperation.Updated && context.TargetStage != null)
            {
                // For updated tasks, we could try to restore the previous status
                // However, this is complex without storing the original status
                // For now, we'll log the limitation and return true
                _logger.LogWarning("Cannot fully rollback task status update for asset {AssetId} - original status not stored",
                    context.Asset.AssetId);
                return true;
            }

            _logger.LogInformation("No rollback operations needed for task management");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback task management operations for asset {AssetId}",
                context.Asset.AssetId);
            return false;
        }
    }

    /// <summary>
    /// Gets the appropriate "ready" task status based on the workflow stage type.
    /// </summary>
    private static TaskStatus GetReadyStatusForStageType(WorkflowStageType stageType)
    {
        return stageType switch
        {
            WorkflowStageType.ANNOTATION => TaskStatus.READY_FOR_ANNOTATION,
            WorkflowStageType.REVISION => TaskStatus.READY_FOR_REVIEW,
            WorkflowStageType.COMPLETION => TaskStatus.READY_FOR_COMPLETION,
            _ => TaskStatus.NOT_STARTED // Fallback for unknown stage types
        };
    }
}