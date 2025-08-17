using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface IWorkflowStageRepository : IGenericRepository<WorkflowStage>
{
    /// <summary>
    /// Gets the next workflow stage for a task based on workflow stage connections.
    /// </summary>
    /// <param name="currentStageId">The ID of the current workflow stage.</param>
    /// <param name="condition">Optional condition for stage transition (e.g., "approved", "rejected").</param>
    /// <returns>The next workflow stage if found, otherwise null.</returns>
    Task<WorkflowStage?> GetNextWorkflowStageAsync(int currentStageId, string? condition = null);

    /// <summary>
    /// Gets the initial workflow stage for a workflow (annotation stage).
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>The initial workflow stage if found, otherwise null.</returns>
    Task<WorkflowStage?> GetInitialWorkflowStageAsync(int workflowId);

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
    /// Checks if a data source is already being used by other workflows.
    /// Each data source should be used by only one workflow stage for data integrity.
    /// </summary>
    /// <param name="dataSourceId">The data source ID to check.</param>
    /// <param name="excludeWorkflowId">Optional workflow ID to exclude from the check (for updates).</param>
    /// <returns>List of conflicting workflow stages using this data source.</returns>
    Task<IEnumerable<WorkflowStage>> GetConflictingDataSourceUsageAsync(int dataSourceId, int? excludeWorkflowId = null);
}
