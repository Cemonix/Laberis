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
/// Pipeline responsible for handling task veto operations.
/// Orchestrates the backward flow: IN_PROGRESS → VETOED → Asset Transfer Back → Annotation Task Update (CHANGES_REQUIRED)
/// </summary>
public class TaskVetoPipeline : ITaskVetoPipeline
{
    private readonly ITaskRepository _taskRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IWorkflowStageResolver _stageResolver;
    private readonly ITaskStatusUpdateStep _statusUpdateStep;
    private readonly IAssetTransferStep _assetTransferStep;
    private readonly ITaskManagementStep _taskManagementStep;
    private readonly IManagementAlertService _alertService;
    private readonly ILogger<ITaskVetoPipeline> _logger;

    public TaskVetoPipeline(
        ITaskRepository taskRepository,
        IAssetRepository assetRepository,
        IWorkflowStageRepository workflowStageRepository,
        IWorkflowStageResolver stageResolver,
        ITaskStatusUpdateStep statusUpdateStep,
        IAssetTransferStep assetTransferStep,
        ITaskManagementStep taskManagementStep,
        IManagementAlertService alertService,
        ILogger<ITaskVetoPipeline> logger)
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

    /// <summary>
    /// Validates if the given task can be vetoed by the specified user.
    /// </summary>
    public async Task<bool> CanExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for veto validation", taskId);
                return false;
            }

            // Only IN_PROGRESS tasks can be vetoed
            if (task.Status != TaskStatus.IN_PROGRESS)
            {
                _logger.LogInformation("Task {TaskId} has status {Status}, cannot be vetoed", taskId, task.Status);
                return false;
            }

            // Cannot veto annotation tasks (first stage)
            var workflowStage = await _workflowStageRepository.GetByIdAsync(task.WorkflowStageId);
            if (workflowStage?.StageType == WorkflowStageType.ANNOTATION)
            {
                _logger.LogWarning("Cannot veto annotation task {TaskId}", taskId);
                return false;
            }

            // Check if user is assigned to the task
            if (task.AssignedToUserId != userId)
            {
                _logger.LogWarning("User {UserId} is not assigned to task {TaskId}", userId, taskId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating veto permissions for task {TaskId} by user {UserId}", taskId, userId);
            return false;
        }
    }

    /// <summary>
    /// Executes the task veto pipeline for the specified task.
    /// </summary>
    public async Task<PipelineResult> ExecuteAsync(int taskId, string userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting veto pipeline for task {TaskId} by user {UserId}", taskId, userId);

            // Get task first for more detailed error messages
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return PipelineResult.Failure($"Task {taskId} not found");
            }

            // Cannot veto annotation tasks (first stage)
            var workflowStage = await _workflowStageRepository.GetByIdAsync(task.WorkflowStageId);
            if (workflowStage?.StageType == WorkflowStageType.ANNOTATION)
            {
                return PipelineResult.Failure("Annotation tasks cannot be vetoed");
            }

            // Check task status first for more specific error messages
            if (task.Status == TaskStatus.VETOED)
            {
                return PipelineResult.Failure("Task has already been vetoed");
            }
            
            // Validate remaining permissions
            if (!await CanExecuteAsync(taskId, userId, cancellationToken))
            {
                return PipelineResult.Failure("User is not authorized to veto this task");
            }

            var asset = await _assetRepository.GetByIdAsync(task.AssetId);
            if (asset == null)
            {
                return PipelineResult.Failure($"Asset {task.AssetId} not found");
            }

            // workflowStage was already fetched above, reuse it as currentStage
            var currentStage = workflowStage;

            // Ensure currentStage is not null before accessing WorkflowId
            if (currentStage == null)
            {
                await CreateManagementAlert(AlertType.DATA_INTEGRITY_VIOLATION, task, asset, userId,
                    "Current workflow stage is null", "Cannot determine workflow for veto operation");
                return PipelineResult.Failure("Current workflow stage is null");
            }

            // Get the first annotation stage (target for veto)
            var annotationStage = await _stageResolver.GetFirstAnnotationStageAsync(currentStage.WorkflowId, cancellationToken);
            if (annotationStage == null)
            {
                await CreateManagementAlert(AlertType.DATA_INTEGRITY_VIOLATION, task, asset, userId, 
                    "No annotation stage found in workflow", "Cannot find annotation stage for veto operation");
                return PipelineResult.Failure("No annotation stage found in workflow");
            }

            // Create pipeline context
            var context = new PipelineContext(task, asset, currentStage, userId, reason)
                .WithTargetStage(annotationStage);

            // Execute pipeline steps with rollback on failure
            var executedSteps = new List<IPipelineStep>();
            
            try
            {
                // Step 1: Update task status to VETOED
                context = await _statusUpdateStep.UpdateStatusAsync(context, TaskStatus.VETOED, cancellationToken);
                executedSteps.Add(_statusUpdateStep);

                // Step 2: Transfer asset back to annotation stage
                context = await _assetTransferStep.TransferAssetToAnnotationAsync(context, cancellationToken);
                executedSteps.Add(_assetTransferStep);

                // Step 3: Update annotation task for changes required
                context = await _taskManagementStep.UpdateAnnotationTaskForChangesAsync(context, cancellationToken);
                executedSteps.Add(_taskManagementStep);

                _logger.LogInformation("Veto pipeline completed successfully for task {TaskId}", taskId);
                return PipelineResult.Success(context.Task);
            }
            catch (Exception stepException)
            {
                _logger.LogError(stepException, "Pipeline step failed, initiating rollback for task {TaskId}", taskId);

                // Attempt rollback in reverse order
                await RollbackSteps(executedSteps, context, cancellationToken);
                
                return PipelineResult.Failure($"Pipeline failed: {stepException.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veto pipeline failed for task {TaskId} by user {UserId}", taskId, userId);
            return PipelineResult.Failure($"Pipeline execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Rolls back executed pipeline steps in reverse order.
    /// </summary>
    private async System.Threading.Tasks.Task RollbackSteps(List<IPipelineStep> executedSteps, PipelineContext context, CancellationToken cancellationToken)
    {
        var rollbackErrors = new List<string>();

        for (int i = executedSteps.Count - 1; i >= 0; i--)
        {
            try
            {
                var success = await executedSteps[i].RollbackAsync(context, cancellationToken);
                if (!success)
                {
                    rollbackErrors.Add($"{executedSteps[i].StepName} rollback failed");
                }
            }
            catch (Exception ex)
            {
                rollbackErrors.Add($"{executedSteps[i].StepName} rollback error: {ex.Message}");
            }
        }

        if (rollbackErrors.Any())
        {
            var rollbackErrorMessage = string.Join("; ", rollbackErrors);
            _logger.LogCritical("Rollback failures for task {TaskId}: {Errors}", context.Task.TaskId, rollbackErrorMessage);
            
            await CreateManagementAlert(
                AlertType.PIPELINE_ROLLBACK_FAILED,
                context.Task,
                context.Asset,
                context.UserId,
                "Veto pipeline rollback failed",
                rollbackErrorMessage);
        }
    }

    /// <summary>
    /// Creates a management alert for critical pipeline failures.
    /// </summary>
    private async System.Threading.Tasks.Task CreateManagementAlert(AlertType type, LaberisTask task, Asset asset, string userId, string reason, string error)
    {
        try
        {
            var alert = await _alertService.CreateAlertAsync(type, task.TaskId, asset.AssetId, userId, reason, error);
            await _alertService.SendCriticalNotificationsAsync(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create management alert for task {TaskId}", task.TaskId);
        }
    }
}