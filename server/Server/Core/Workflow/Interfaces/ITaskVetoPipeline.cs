using server.Core.Workflow.Models;

namespace server.Core.Workflow.Interfaces;

/// <summary>
/// Pipeline responsible for handling task veto operations.
/// Manages the backward flow: IN_PROGRESS → VETOED → Asset Transfer Back → Annotation Task Update (CHANGES_REQUIRED)
/// </summary>
public interface ITaskVetoPipeline
{
    /// <summary>
    /// Executes the task veto pipeline for the specified task.
    /// </summary>
    /// <param name="taskId">The ID of the task to veto.</param>
    /// <param name="userId">The ID of the user vetoing the task.</param>
    /// <param name="reason">Optional reason for the veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and updated entities.</returns>
    Task<PipelineResult> ExecuteAsync(int taskId, string userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the given task can be vetoed by the specified user.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting veto.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be vetoed, false otherwise.</returns>
    Task<bool> CanExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default);
}