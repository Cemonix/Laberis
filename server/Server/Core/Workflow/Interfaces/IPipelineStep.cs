using server.Core.Workflow.Models;

namespace server.Core.Workflow.Interfaces;

/// <summary>
/// Represents a single step in a workflow pipeline.
/// Each step performs a specific operation and can provide rollback functionality.
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Executes this pipeline step with the given context.
    /// </summary>
    /// <param name="context">The pipeline context containing all necessary data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with any modifications made by this step.</returns>
    Task<PipelineContext> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the changes made by this step.
    /// Called when the pipeline fails and needs to be reversed.
    /// </summary>
    /// <param name="context">The pipeline context from when this step was executed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if rollback was successful, false otherwise.</returns>
    Task<bool> RollbackAsync(PipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// The name of this pipeline step for logging and error reporting.
    /// </summary>
    string StepName { get; }
}