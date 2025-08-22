using server.Core.Workflow.Interfaces;
using server.Core.Workflow.Models;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Models.Domain.Enums;

namespace server.Services;

/// <summary>
/// High-level service for managing task workflow operations.
/// Orchestrates pipeline execution for task completion and veto operations.
/// </summary>
public class TaskWorkflowService : ITaskWorkflowService
{
    private readonly ITaskCompletionPipeline _completionPipeline;
    private readonly ITaskVetoPipeline _vetoPipeline;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectMembershipService _projectMembershipService;
    private readonly ILogger<TaskWorkflowService> _logger;

    public TaskWorkflowService(
        ITaskCompletionPipeline completionPipeline,
        ITaskVetoPipeline vetoPipeline,
        ITaskRepository taskRepository,
        IProjectMembershipService projectMembershipService,
        ILogger<TaskWorkflowService> logger)
    {
        _completionPipeline = completionPipeline ?? throw new ArgumentNullException(nameof(completionPipeline));
        _vetoPipeline = vetoPipeline ?? throw new ArgumentNullException(nameof(vetoPipeline));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _projectMembershipService = projectMembershipService ?? throw new ArgumentNullException(nameof(projectMembershipService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Completes a task and triggers the forward workflow progression.
    /// This includes status update, asset transfer, and next stage task creation.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="userId">The ID of the user completing the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and affected entities.</returns>
    public async Task<PipelineResult> CompleteTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating task completion pipeline for task {TaskId} by user {UserId}", taskId, userId);

        try
        {
            // Validate permissions before executing pipeline
            if (!await CanCompleteTaskAsync(taskId, userId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} does not have permission to complete task {TaskId}", userId, taskId);
                return PipelineResult.Failure("Insufficient permissions to complete this task");
            }

            // Execute the completion pipeline
            var result = await _completionPipeline.ExecuteAsync(taskId, userId, cancellationToken);
            
            _logger.LogInformation("Task completion pipeline result for task {TaskId}: {IsSuccess}", taskId, result.IsSuccess);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing task completion pipeline for task {TaskId}", taskId);
            return PipelineResult.Failure($"Pipeline execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Vetoes a task and triggers the backward workflow progression.
    /// This includes status update, asset transfer back to annotation, and annotation task update.
    /// </summary>
    /// <param name="taskId">The ID of the task to veto.</param>
    /// <param name="userId">The ID of the user vetoing the task.</param>
    /// <param name="reason">Optional reason for the veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and affected entities.</returns>
    public async Task<PipelineResult> VetoTaskAsync(int taskId, string userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating task veto pipeline for task {TaskId} by user {UserId} with reason: {Reason}", taskId, userId, reason);

        try
        {
            // Validate permissions before executing pipeline
            if (!await CanVetoTaskAsync(taskId, userId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} does not have permission to veto task {TaskId}", userId, taskId);
                return PipelineResult.Failure("Insufficient permissions to veto this task");
            }

            // Execute the veto pipeline
            var result = await _vetoPipeline.ExecuteAsync(taskId, userId, reason, cancellationToken);
            
            _logger.LogInformation("Task veto pipeline result for task {TaskId}: {IsSuccess}", taskId, result.IsSuccess);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing task veto pipeline for task {TaskId}", taskId);
            return PipelineResult.Failure($"Pipeline execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates if a task can be completed by the specified user.
    /// Checks permissions, task status, and workflow constraints.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting completion.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be completed, false otherwise.</returns>
    public async Task<bool> CanCompleteTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user has permission to complete tasks using the pipeline
            if (!await _completionPipeline.CanExecuteAsync(taskId, userId, cancellationToken))
            {
                return false;
            }

            // Additional business logic validation can be added here
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for completion validation", taskId);
                return false;
            }

            // Validate user has appropriate project role
            var membership = await _projectMembershipService.GetProjectMembershipAsync(userId, task.ProjectId);
            if (membership == null)
            {
                _logger.LogWarning("User {UserId} is not a member of project {ProjectId}", userId, task.ProjectId);
                return false;
            }

            // Only annotators and reviewers can complete tasks
            if (membership.Role == ProjectRole.VIEWER)
            {
                _logger.LogWarning("User {UserId} with role {Role} cannot complete tasks", userId, membership.Role);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating task completion permissions for task {TaskId} and user {UserId}", taskId, userId);
            return false;
        }
    }

    /// <summary>
    /// Validates if a task can be vetoed by the specified user.
    /// Checks permissions, task status, and workflow constraints.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be vetoed, false otherwise.</returns>
    public async Task<bool> CanVetoTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user has permission to veto tasks using the pipeline
            if (!await _vetoPipeline.CanExecuteAsync(taskId, userId, cancellationToken))
            {
                return false;
            }

            // Additional business logic validation can be added here
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for veto validation", taskId);
                return false;
            }

            // Validate user has appropriate project role
            var membership = await _projectMembershipService.GetProjectMembershipAsync(userId, task.ProjectId);
            if (membership == null)
            {
                _logger.LogWarning("User {UserId} is not a member of project {ProjectId}", userId, task.ProjectId);
                return false;
            }

            // Only reviewers and managers can veto tasks
            if (membership.Role != ProjectRole.REVIEWER && membership.Role != ProjectRole.MANAGER)
            {
                _logger.LogWarning("User {UserId} with role {Role} cannot veto tasks", userId, membership.Role);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating task veto permissions for task {TaskId} and user {UserId}", taskId, userId);
            return false;
        }
    }
}