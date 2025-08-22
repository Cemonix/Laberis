using server.Core.Workflow.Models;

namespace server.Core.Workflow.Interfaces.Steps;

/// <summary>
/// Pipeline step responsible for transferring assets between data sources during workflow progression.
/// </summary>
public interface IAssetTransferStep : IPipelineStep
{
    /// <summary>
    /// Transfers the asset from its current data source to the target workflow stage's data source.
    /// </summary>
    /// <param name="context">The pipeline context containing the asset and target stage information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with any transfer-related data.</returns>
    Task<PipelineContext> TransferAssetAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers the asset back to the first annotation stage data source (used for veto operations).
    /// </summary>
    /// <param name="context">The pipeline context containing the asset information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Updated context with transfer information.</returns>
    Task<PipelineContext> TransferAssetToAnnotationAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default);
}