using server.Core.Workflow.Models;

namespace server.Services.Interfaces;

/// <summary>
/// High-level service for managing task workflow operations.
/// Orchestrates pipeline execution for task completion and veto operations.
/// </summary>
public interface ITaskWorkflowService
{
    /// <summary>
    /// Completes a task and triggers the forward workflow progression.
    /// This includes status update, asset transfer, and next stage task creation.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="userId">The ID of the user completing the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and affected entities.</returns>
    Task<PipelineResult> CompleteTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vetoes a task and triggers the backward workflow progression.
    /// This includes status update, asset transfer back to annotation, and annotation task update.
    /// </summary>
    /// <param name="taskId">The ID of the task to veto.</param>
    /// <param name="userId">The ID of the user vetoing the task.</param>
    /// <param name="reason">Optional reason for the veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and affected entities.</returns>
    Task<PipelineResult> VetoTaskAsync(int taskId, string userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a task can be completed by the specified user.
    /// Checks permissions, task status, and workflow constraints.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting completion.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be completed, false otherwise.</returns>
    Task<bool> CanCompleteTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a task can be vetoed by the specified user.
    /// Checks permissions, task status, and workflow constraints.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be vetoed, false otherwise.</returns>
    Task<bool> CanVetoTaskAsync(int taskId, string userId, CancellationToken cancellationToken = default);
}