using server.Services.Interfaces;
using server.Models.Internal;
using server.Repositories.Interfaces;
using server.Models.Domain;

namespace server.Services.EventHandlers;

/// <summary>
/// Handles asset imported events by creating appropriate tasks
/// </summary>
public class AssetImportedEventHandler : IDomainEventHandler<AssetImportedEvent>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly ILogger<AssetImportedEventHandler> _logger;

    public AssetImportedEventHandler(
        ITaskRepository taskRepository,
        IWorkflowStageRepository workflowStageRepository,
        ILogger<AssetImportedEventHandler> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async System.Threading.Tasks.Task HandleAsync(AssetImportedEvent domainEvent)
    {
        var asset = domainEvent.Asset;
        
        _logger.LogInformation("Handling asset imported event for asset {AssetId} in data source {DataSourceId}", 
            asset.AssetId, asset.DataSourceId);

        try
        {
            // Find all workflow stages where InputDataSourceId == asset.DataSourceId
            var workflowStages = await _workflowStageRepository.FindAsync(
                ws => ws.InputDataSourceId == asset.DataSourceId && 
                      ws.Workflow.ProjectId == asset.ProjectId);

            var relevantStages = workflowStages.ToList();
            
            if (relevantStages.Count == 0)
            {
                _logger.LogInformation("No workflow stages found that use data source {DataSourceId} as input", 
                    asset.DataSourceId);
                return;
            }

            _logger.LogInformation("Found {StageCount} workflow stages that use data source {DataSourceId} as input", 
                relevantStages.Count, asset.DataSourceId);

            // Create tasks for each relevant workflow stage
            foreach (var stage in relevantStages)
            {
                await CreateTaskForAssetInStage(asset, stage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle asset imported event for asset {AssetId}", asset.AssetId);
            throw; // Re-throw to ensure the event system knows about the failure
        }
    }

    private async System.Threading.Tasks.Task CreateTaskForAssetInStage(Asset asset, WorkflowStage stage)
    {
        try
        {
            _logger.LogInformation("Creating task for asset {AssetId} in workflow stage {StageId} ({StageName})", 
                asset.AssetId, stage.WorkflowStageId, stage.Name);

            var task = new Models.Domain.Task
            {
                Priority = 1, // Default priority
                AssetId = asset.AssetId,
                ProjectId = asset.ProjectId,
                WorkflowId = stage.WorkflowId,
                CurrentWorkflowStageId = stage.WorkflowStageId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();
            
            _logger.LogInformation("Successfully created task {TaskId} for asset {AssetId} in workflow stage {StageId}", 
                task.TaskId, asset.AssetId, stage.WorkflowStageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task for asset {AssetId} in workflow stage {StageId}", 
                asset.AssetId, stage.WorkflowStageId);
            // Don't throw here - we want to continue with other stages
        }
    }
}