using server.Core.Workflow.Models;
using LaberisTask = server.Models.Domain.Task;

namespace server.Core.Workflow.Interfaces.Steps;

/// <summary>
/// Pipeline step responsible for managing task creation and updates during workflow operations.
/// </summary>
public interface ITaskManagementStep : IPipelineStep
{
    /// <summary>
    /// Creates or updates a task in the target workflow stage for the asset.
    /// Used during forward progression (task completion).
    /// </summary>
    /// <param name="context">The pipeline context containing asset and target stage information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with the created or updated task information.</returns>
    Task<PipelineContext> CreateOrUpdateTaskForTargetStageAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds and updates the annotation task for the asset to CHANGES_REQUIRED status.
    /// Used during veto operations to mark tasks that need rework.
    /// </summary>
    /// <param name="context">The pipeline context containing asset information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with the modified annotation task information.</returns>
    Task<PipelineContext> UpdateAnnotationTaskForChangesAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates data integrity by checking task status consistency.
    /// Ensures no conflicting task states exist for the same asset.
    /// </summary>
    /// <param name="context">The pipeline context to validate.</param>
    /// <param name="targetTask">The task that was found or created in the target stage.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if data integrity is valid, false if inconsistencies are found.</returns>
    Task<bool> ValidateDataIntegrityAsync(
        PipelineContext context,
        LaberisTask? targetTask,
        CancellationToken cancellationToken = default);
}