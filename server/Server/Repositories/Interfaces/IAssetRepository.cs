using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface IAssetRepository : IGenericRepository<Asset>
{
    /// <summary>
    /// Gets the count of assets available for task creation in a data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>Count of available assets.</returns>
    Task<int> GetAvailableAssetsCountAsync(int projectId);

    /// <summary>
    /// Gets available assets from a specific data source for task creation.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>Collection of available assets from the data source.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId);

    /// <summary>
    /// Gets all assets in a project that don't have tasks yet and are imported.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowStageId">The ID of the workflow stage to filter assets by its input data source.</param>
    /// <returns>List of assets available for task creation.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsForTaskCreationAsync(int projectId, int? workflowStageId = null);
}
