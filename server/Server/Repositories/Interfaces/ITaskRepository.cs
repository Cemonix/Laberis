using LaberisTask = server.Models.Domain.Task;
using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<LaberisTask>
{
    /// <summary>
    /// Gets all assets in a project that don't have tasks yet and are imported.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>List of assets available for task creation.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsForTaskCreationAsync(int projectId);

    /// <summary>
    /// Gets the count of assets available for task creation in a data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>Count of available assets.</returns>
    Task<int> GetAvailableAssetsCountAsync(int projectId, int? dataSourceId = null);
}
