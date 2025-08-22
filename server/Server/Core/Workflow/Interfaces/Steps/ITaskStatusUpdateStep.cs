using server.Core.Workflow.Models;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Core.Workflow.Interfaces.Steps;

/// <summary>
/// Pipeline step responsible for updating task status during workflow operations.
/// </summary>
public interface ITaskStatusUpdateStep : IPipelineStep
{
    /// <summary>
    /// Updates the task status to the specified target status.
    /// </summary>
    /// <param name="context">The pipeline context containing the task to update.</param>
    /// <param name="targetStatus">The status to set on the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with the modified task.</returns>
    Task<PipelineContext> UpdateStatusAsync(
        PipelineContext context,
        TaskStatus targetStatus,
        CancellationToken cancellationToken = default);
}