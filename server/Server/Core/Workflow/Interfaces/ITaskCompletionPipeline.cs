using server.Core.Workflow.Models;

namespace server.Core.Workflow.Interfaces;

/// <summary>
/// Pipeline responsible for handling task completion operations.
/// Manages the forward flow: IN_PROGRESS → COMPLETED → Asset Transfer → Next Stage Task Creation
/// </summary>
public interface ITaskCompletionPipeline
{
    /// <summary>
    /// Executes the task completion pipeline for the specified task.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="userId">The ID of the user completing the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Pipeline result indicating success/failure and updated entities.</returns>
    Task<PipelineResult> ExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the given task can be completed by the specified user.
    /// </summary>
    /// <param name="taskId">The ID of the task to validate.</param>
    /// <param name="userId">The ID of the user attempting completion.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be completed, false otherwise.</returns>
    Task<bool> CanExecuteAsync(int taskId, string userId, CancellationToken cancellationToken = default);
}