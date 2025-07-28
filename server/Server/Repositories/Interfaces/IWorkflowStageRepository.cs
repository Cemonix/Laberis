using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface IWorkflowStageRepository : IGenericRepository<WorkflowStage>
{
    /// <summary>
    /// Gets all workflow stages for a specific workflow with their connections included.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, containing all stages with connections.</returns>
    Task<IEnumerable<WorkflowStage>> GetWorkflowStagesWithConnectionsAsync(int workflowId);
    
    /// <summary>
    /// Gets a workflow stage by its ID with connections and assignments included.
    /// </summary>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing the stage with connections if found.</returns>
    Task<WorkflowStage?> GetStageWithConnectionsAsync(int stageId);

    /// <summary>
    /// Checks if a data source is already being used by other workflows (excluding completion stages).
    /// </summary>
    /// <param name="dataSourceId">The data source ID to check.</param>
    /// <param name="excludeWorkflowId">Optional workflow ID to exclude from the check (for updates).</param>
    /// <returns>List of conflicting workflow stages using this data source.</returns>
    Task<IEnumerable<WorkflowStage>> GetConflictingDataSourceUsageAsync(int dataSourceId, int? excludeWorkflowId = null);
}
