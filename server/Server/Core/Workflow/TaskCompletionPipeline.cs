using Microsoft.Extensions.Logging;
using server.Core.Alerts.Interfaces;
using server.Core.Alerts.Models;
using server.Core.Workflow.Interfaces;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Core.Workflow;

/// <summary>
/// Pipeline responsible for handling task completion operations.
/// Orchestrates the forward flow: IN_PROGRESS → COMPLETED → Asset Transfer → Next Stage Task Creation
/// </summary>
public class TaskCompletionPipeline : ITaskCompletionPipeline
{
    private readonly ITaskRepository _taskRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IWorkflowStageResolver _stageResolver;
    private readonly ITaskStatusUpdateStep _statusUpdateStep;
    private readonly IAssetTransferStep _assetTransferStep;
    private readonly ITaskManagementStep _taskManagementStep;
    private readonly IManagementAlertService _alertService;
    private readonly ILogger<ITaskCompletionPipeline> _logger;

    public TaskCompletionPipeline(
        ITaskRepository taskRepository,
        IAssetRepository assetRepository,
        IWorkflowStageRepository workflowStageRepository,
        IWorkflowStageResolver stageResolver,
        ITaskStatusUpdateStep statusUpdateStep,
        IAssetTransferStep assetTransferStep,
        ITaskManagementStep taskManagementStep,
        IManagementAlertService alertService,
        ILogger<ITaskCompletionPipeline> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _stageResolver = stageResolver ?? throw new ArgumentNullException(nameof(stageResolver));
        _statusUpdateStep = statusUpdateStep ?? throw new ArgumentNullException(nameof(statusUpdateStep));
        _assetTransferStep = assetTransferStep ?? throw new ArgumentNullException(nameof(assetTransferStep));
        _taskManagementStep = taskManagementStep ?? throw new ArgumentNullException(nameof(taskManagementStep));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PipelineResult> ExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting task completion pipeline for task {TaskId} by user {UserId}", taskId, userId);

        try
        {
            // Step 1: Validate task existence and permissions
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return PipelineResult.Failure($"Task not found with ID: {taskId}");
            }

            // Step 2: Validate task can be completed
            if (!CanCompleteTask(task, userId))
            {
                return PipelineResult.Failure($"Task {taskId} cannot be completed by user {userId}. Current status: {task.Status}");
            }

            // Step 3: Get associated entities
            var asset = await _assetRepository.GetByIdAsync(task.AssetId);
            if (asset == null)
            {
                return PipelineResult.Failure($"Asset not found with ID: {task.AssetId}");
            }

            var currentStage = await _workflowStageRepository.GetByIdAsync(task.WorkflowStageId);
            if (currentStage == null)
            {
                return PipelineResult.Failure($"Workflow stage not found with ID: {task.WorkflowStageId}");
            }

            // Step 4: Create pipeline context
            var context = new PipelineContext(task, asset, currentStage, userId);

            // Step 5: Determine next stage (if any)
            var nextStage = await _stageResolver.GetNextStageAsync(currentStage.WorkflowStageId, cancellationToken);
            if (nextStage != null)
            {
                context = context.WithTargetStage(nextStage);
            }

            // Step 6: Execute pipeline steps in sequence with rollback capability
            var executedSteps = new List<IPipelineStep>();
            
            try
            {
                // Step 6a: Update task status to COMPLETED
                _logger.LogInformation("Updating task {TaskId} status to COMPLETED", taskId);
                context = await _statusUpdateStep.UpdateStatusAsync(context, TaskStatus.COMPLETED, cancellationToken);
                executedSteps.Add(_statusUpdateStep);

                // Step 6b: Transfer asset to next stage (if applicable)
                if (nextStage != null)
                {
                    _logger.LogInformation("Transferring asset {AssetId} to next stage {StageId}", asset.AssetId, nextStage.WorkflowStageId);
                    context = await _assetTransferStep.TransferAssetAsync(context, cancellationToken);
                    executedSteps.Add(_assetTransferStep);

                    // Step 6c: Create/update task for next stage
                    _logger.LogInformation("Creating task for asset {AssetId} in next stage {StageId}", asset.AssetId, nextStage.WorkflowStageId);
                    context = await _taskManagementStep.CreateOrUpdateTaskForTargetStageAsync(context, cancellationToken);
                    executedSteps.Add(_taskManagementStep);
                }

                // Step 7: Get the final state of entities
                var updatedTask = await _taskRepository.GetByIdAsync(taskId);
                LaberisTask? createdTask = null;
                
                if (nextStage != null)
                {
                    createdTask = await _taskRepository.FindByAssetAndStageAsync(asset.AssetId, nextStage.WorkflowStageId);
                }

                _logger.LogInformation("Task completion pipeline completed successfully for task {TaskId}", taskId);
                return PipelineResult.Success(updatedTask ?? task, createdTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline execution failed for task {TaskId}. Initiating rollback.", taskId);
                
                // Attempt rollback of executed steps in reverse order
                await RollbackStepsAsync(executedSteps, context, ex);
                
                return PipelineResult.Failure($"Task completion failed: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in task completion pipeline for task {TaskId}", taskId);
            return PipelineResult.Failure($"Critical pipeline error: {ex.Message}", ex);
        }
    }

    public async Task<bool> CanExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            return task != null && CanCompleteTask(task, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking execution permissions for task {TaskId} and user {UserId}", taskId, userId);
            return false;
        }
    }

    private static bool CanCompleteTask(LaberisTask task, string userId)
    {
        // Task must be in IN_PROGRESS status to be completed
        if (task.Status != TaskStatus.IN_PROGRESS)
        {
            return false;
        }

        // Task must be assigned to the user attempting completion
        return task.AssignedToUserId == userId;
    }

    private async System.Threading.Tasks.Task RollbackStepsAsync(
        List<IPipelineStep> executedSteps, 
        PipelineContext context, 
        Exception originalException)
    {
        var rollbackErrors = new List<string>();

        // Execute rollback in reverse order
        for (int i = executedSteps.Count - 1; i >= 0; i--)
        {
            var step = executedSteps[i];
            try
            {
                _logger.LogInformation("Rolling back step: {StepName}", step.StepName);
                var rollbackResult = await step.RollbackAsync(context);
                
                if (!rollbackResult)
                {
                    rollbackErrors.Add($"{step.StepName} rollback failed");
                }
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Exception during rollback of step: {StepName}", step.StepName);
                rollbackErrors.Add($"{step.StepName} rollback threw exception: {rollbackEx.Message}");
            }
        }

        // If any rollback failed, create a management alert
        if (rollbackErrors.Count != 0)
        {
            var rollbackError = string.Join("; ", rollbackErrors);
            _logger.LogCritical("Pipeline rollback failed for task {TaskId}. Original error: {OriginalError}. Rollback errors: {RollbackErrors}",
                context.Task.TaskId, originalException.Message, rollbackError);

            try
            {
                var alert = await _alertService.CreateAlertAsync(
                    AlertType.PIPELINE_ROLLBACK_FAILED,
                    context.Task.TaskId,
                    context.Asset.AssetId,
                    context.UserId,
                    "Task completion pipeline rollback failed",
                    originalException.Message,
                    rollbackError);

                await _alertService.SendCriticalNotificationsAsync(alert);
            }
            catch (Exception alertEx)
            {
                _logger.LogCritical(alertEx, "Failed to create management alert for failed rollback. This requires immediate manual intervention.");
            }
        }
        else
        {
            _logger.LogInformation("All pipeline steps rolled back successfully for task {TaskId}", context.Task.TaskId);
        }
    }
}