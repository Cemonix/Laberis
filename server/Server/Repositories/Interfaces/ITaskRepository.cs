using LaberisTask = server.Models.Domain.Task;
using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<LaberisTask>
{
    /// <summary>
    /// Gets all assets in a project that don't have tasks yet and are imported.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowStageId">The ID of the workflow stage to filter assets by its input data source.</param>
    /// <returns>List of assets available for task creation.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsForTaskCreationAsync(int projectId, int? workflowStageId = null);

    /// <summary>
    /// Gets the count of assets available for task creation in a data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>Count of available assets.</returns>
    Task<int> GetAvailableAssetsCountAsync(int projectId, int? dataSourceId = null);

    /// <summary>
    /// Gets available assets from a specific data source for task creation.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>Collection of available assets from the data source.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId);

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
}
