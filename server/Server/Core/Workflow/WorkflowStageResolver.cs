using server.Core.Workflow.Interfaces;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace server.Core.Workflow;

/// <summary>
/// Service responsible for resolving workflow stage relationships and connections.
/// </summary>
public class WorkflowStageResolver : IWorkflowStageResolver
{
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IWorkflowStageConnectionRepository _connectionRepository;
    private readonly ILogger<WorkflowStageResolver> _logger;

    public WorkflowStageResolver(
        IWorkflowStageRepository workflowStageRepository,
        IWorkflowStageConnectionRepository connectionRepository,
        ILogger<WorkflowStageResolver> logger)
    {
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the next workflow stage in the pipeline for task completion.
    /// Uses WorkflowStageConnection to find the connected stage.
    /// </summary>
    /// <param name="currentStageId">The ID of the current workflow stage.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The next workflow stage, or null if current stage is final.</returns>
    public async Task<WorkflowStage?> GetNextStageAsync(int currentStageId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving next stage for workflow stage {CurrentStageId}", currentStageId);

        try
        {
            // Get the connection from the current stage to the next stage
            var connections = await _connectionRepository.GetOutgoingConnectionsAsync(currentStageId);
            var connection = connections.FirstOrDefault();

            if (connection == null)
            {
                _logger.LogDebug("No connections found from stage {CurrentStageId}, assuming final stage", currentStageId);
                return null;
            }

            // Get the target stage
            var nextStage = await _workflowStageRepository.GetByIdAsync(connection.ToStageId);
            _logger.LogDebug("Found next stage {NextStageId} ({StageType}) for current stage {CurrentStageId}", 
                nextStage?.WorkflowStageId, nextStage?.StageType, currentStageId);

            return nextStage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving next stage for workflow stage {CurrentStageId}", currentStageId);
            throw;
        }
    }

    /// <summary>
    /// Gets the first annotation workflow stage in the workflow.
    /// Used for veto operations to return assets to the beginning of the process.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to search in.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The first annotation stage in the workflow.</returns>
    public async Task<WorkflowStage> GetFirstAnnotationStageAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding first annotation stage in workflow {WorkflowId}", workflowId);

        try
        {
            var stages = await _workflowStageRepository.GetWorkflowStagesWithConnectionsAsync(workflowId);
            var annotationStage = stages
                .Where(s => s.StageType == WorkflowStageType.ANNOTATION)
                .OrderBy(s => s.StageOrder)
                .FirstOrDefault();

            if (annotationStage == null)
            {
                var error = $"No annotation stage found in workflow {workflowId}";
                _logger.LogError("No annotation stage found in workflow {WorkflowId}", workflowId);
                throw new InvalidOperationException(error);
            }

            _logger.LogDebug("Found first annotation stage {StageId} (Order: {StageOrder}) in workflow {WorkflowId}", 
                annotationStage.WorkflowStageId, annotationStage.StageOrder, workflowId);

            return annotationStage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding first annotation stage in workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    /// <summary>
    /// Gets all workflow stages that lead to the completion stage.
    /// Used to handle the specialty case where completion stage can have multiple predecessors.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to search in.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of stages that can feed into the completion stage.</returns>
    public async Task<IEnumerable<WorkflowStage>> GetCompletionPredecessorsAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding stages that lead to completion in workflow {WorkflowId}", workflowId);

        try
        {
            var stages = await _workflowStageRepository.GetWorkflowStagesWithConnectionsAsync(workflowId);
            var completionStage = stages.FirstOrDefault(s => s.StageType == WorkflowStageType.COMPLETION);

            if (completionStage == null)
            {
                _logger.LogWarning("No completion stage found in workflow {WorkflowId}", workflowId);
                return [];
            }

            // Get all connections that lead to the completion stage
            var connectionsToCompletion = await _connectionRepository.GetIncomingConnectionsAsync(completionStage.WorkflowStageId);
            var predecessorStageIds = connectionsToCompletion.Select(c => c.FromStageId).ToList();

            if (predecessorStageIds.Count == 0)
            {
                _logger.LogWarning("No stages found that connect to completion stage {CompletionStageId} in workflow {WorkflowId}", 
                    completionStage.WorkflowStageId, workflowId);
                return [];
            }

            var predecessorStages = stages.Where(s => predecessorStageIds.Contains(s.WorkflowStageId)).ToList();
            
            _logger.LogDebug("Found {Count} predecessor stages to completion in workflow {WorkflowId}: [{StageIds}]", 
                predecessorStages.Count, workflowId, string.Join(", ", predecessorStages.Select(s => s.WorkflowStageId)));

            return predecessorStages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding completion predecessors in workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    /// <summary>
    /// Validates if a workflow stage connection exists between two stages.
    /// </summary>
    /// <param name="fromStageId">The source stage ID.</param>
    /// <param name="toStageId">The target stage ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if a connection exists, false otherwise.</returns>
    public async Task<bool> ConnectionExistsAsync(int fromStageId, int toStageId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if connection exists from stage {FromStageId} to stage {ToStageId}", fromStageId, toStageId);

        try
        {
            var connections = await _connectionRepository.GetOutgoingConnectionsAsync(fromStageId);
            var connectionExists = connections.Any(c => c.ToStageId == toStageId);

            _logger.LogDebug("Connection from stage {FromStageId} to stage {ToStageId} exists: {Exists}", 
                fromStageId, toStageId, connectionExists);

            return connectionExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connection from stage {FromStageId} to stage {ToStageId}", fromStageId, toStageId);
            throw;
        }
    }
}