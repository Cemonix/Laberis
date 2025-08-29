using Microsoft.Extensions.Logging;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace server.Core.Workflow.Steps;

/// <summary>
/// Pipeline step responsible for transferring assets between data sources during workflow progression.
/// Implements atomic transfer operations with proper rollback capability for pipeline consistency.
/// </summary>
public class AssetTransferStep : IAssetTransferStep
{
    private readonly IAssetService _assetService;
    private readonly IDataSourceService _dataSourceService;
    private readonly ILogger<IAssetTransferStep> _logger;
    
    // Store original data source ID for rollback purposes
    private int? _originalDataSourceId;

    public AssetTransferStep(
        IAssetService assetService,
        IDataSourceService dataSourceService,
        ILogger<IAssetTransferStep> logger)
    {
        _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StepName => "AssetTransferStep";

    public async Task<PipelineContext> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {StepName} for asset {AssetId}", StepName, context.Asset.AssetId);
        
        // Default behavior: transfer to target stage's data source
        return await TransferAssetAsync(context, cancellationToken);
    }

    public async Task<PipelineContext> TransferAssetAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TargetStage == null)
        {
            throw new InvalidOperationException("Target stage is required for asset transfer");
        }

        if (context.CurrentStage.TargetDataSourceId == null)
        {
            throw new InvalidOperationException("Target data source is required for asset transfer");
        }

        _logger.LogInformation("Transferring asset {AssetId} from data source {FromDataSource} to {ToDataSource}",
            context.Asset.AssetId, context.Asset.DataSourceId, context.CurrentStage.TargetDataSourceId.Value);

        // Store original data source ID for potential rollback
        _originalDataSourceId = context.Asset.DataSourceId;

        try
        {
            var transferResult = await _assetService.TransferAssetToDataSourceAsync(
                context.Asset.AssetId,
                context.CurrentStage.TargetDataSourceId.Value);

            if (!transferResult)
            {
                throw new InvalidOperationException(
                    $"Failed to transfer asset {context.Asset.AssetId} to data source {context.CurrentStage.TargetDataSourceId.Value}");
            }

            _logger.LogInformation("Successfully transferred asset {AssetId} to data source {DataSourceId}",
                context.Asset.AssetId, context.CurrentStage.TargetDataSourceId.Value);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer asset {AssetId} to data source {DataSourceId}",
                context.Asset.AssetId, context.CurrentStage.TargetDataSourceId.Value);
            throw;
        }
    }

    public async Task<PipelineContext> TransferAssetToAnnotationAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // For veto operations, transfer back to the annotation data source
        // Dynamically find the annotation data source for the project
        var workflowDataSources = await _dataSourceService.EnsureRequiredDataSourcesExistAsync(
            context.Asset.ProjectId, includeReviewStage: false);
        
        if (workflowDataSources.AnnotationDataSource == null)
        {
            throw new InvalidOperationException(
                $"No annotation data source found for project {context.Asset.ProjectId}");
        }
        
        var annotationDataSourceId = workflowDataSources.AnnotationDataSource.Id;

        _logger.LogInformation("Transferring asset {AssetId} back to annotation data source {DataSourceId} for veto",
            context.Asset.AssetId, annotationDataSourceId);

        // Store original data source ID for potential rollback
        _originalDataSourceId = context.Asset.DataSourceId;

        try
        {
            var transferResult = await _assetService.TransferAssetToDataSourceAsync(
                context.Asset.AssetId,
                annotationDataSourceId);

            if (!transferResult)
            {
                throw new InvalidOperationException(
                    $"Failed to transfer asset back to annotation stage");
            }

            _logger.LogInformation("Successfully transferred asset {AssetId} back to annotation data source {DataSourceId}",
                context.Asset.AssetId, annotationDataSourceId);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer asset {AssetId} back to annotation data source",
                context.Asset.AssetId);
            throw;
        }
    }

    public async Task<bool> RollbackAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Determine the rollback target data source
        // If we have a stored original data source, use that. Otherwise, use the current stage's target data source
        int rollbackDataSourceId;
        if (_originalDataSourceId.HasValue)
        {
            rollbackDataSourceId = _originalDataSourceId.Value;
        }
        else if (context.CurrentStage.TargetDataSourceId.HasValue)
        {
            // Fallback: assume we need to rollback to current stage's data source
            rollbackDataSourceId = context.CurrentStage.TargetDataSourceId.Value;
        }
        else
        {
            _logger.LogWarning("No rollback target data source available for asset {AssetId}", context.Asset.AssetId);
            return false;
        }

        _logger.LogInformation("Rolling back asset {AssetId} transfer from {CurrentDataSource} to {RollbackDataSource}",
            context.Asset.AssetId, context.Asset.DataSourceId, rollbackDataSourceId);

        try
        {
            var rollbackResult = await _assetService.TransferAssetToDataSourceAsync(
                context.Asset.AssetId,
                rollbackDataSourceId);

            if (!rollbackResult)
            {
                _logger.LogError("Failed to rollback asset {AssetId} to data source {DataSourceId}",
                    context.Asset.AssetId, rollbackDataSourceId);
                return false;
            }

            _logger.LogInformation("Successfully rolled back asset {AssetId} to data source {DataSourceId}",
                context.Asset.AssetId, rollbackDataSourceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback asset {AssetId} to data source {DataSourceId}",
                context.Asset.AssetId, rollbackDataSourceId);
            return false;
        }
    }
}