using server.Models.Domain;

namespace server.Core.Workflow.Interfaces;

/// <summary>
/// Service responsible for resolving workflow stage relationships and connections.
/// </summary>
public interface IWorkflowStageResolver
{
    /// <summary>
    /// Gets the next workflow stage in the pipeline for task completion.
    /// Uses WorkflowStageConnection to find the connected stage.
    /// </summary>
    /// <param name="currentStageId">The ID of the current workflow stage.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The next workflow stage, or null if current stage is final.</returns>
    Task<WorkflowStage?> GetNextStageAsync(int currentStageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first annotation workflow stage in the workflow.
    /// Used for veto operations to return assets to the beginning of the process.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to search in.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The first annotation stage in the workflow.</returns>
    Task<WorkflowStage> GetFirstAnnotationStageAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workflow stages that lead to the completion stage.
    /// Used to handle the specialty case where completion stage can have multiple predecessors.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to search in.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of stages that can feed into the completion stage.</returns>
    Task<IEnumerable<WorkflowStage>> GetCompletionPredecessorsAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a workflow stage connection exists between two stages.
    /// </summary>
    /// <param name="fromStageId">The source stage ID.</param>
    /// <param name="toStageId">The target stage ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if a connection exists, false otherwise.</returns>
    Task<bool> ConnectionExistsAsync(int fromStageId, int toStageId, CancellationToken cancellationToken = default);
}