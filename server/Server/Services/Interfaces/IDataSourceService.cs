using server.Models.Domain.Enums;
using server.Models.Common;
using server.Models.DTOs.DataSource;

namespace server.Services.Interfaces
{
    public interface IDataSourceService
    {
        /// <summary>
        /// Retrieves all data sources for a specific project, optionally filtered, sorted, and paginated.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="filterOn">The field to filter on.</param>
        /// <param name="filterQuery">The query string to filter by.</param>
        /// <param name="sortBy">The field to sort by.</param>
        /// <param name="isAscending">True for ascending order, false for descending.</param>
        /// <param name="pageNumber">The page number for pagination (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of DataSourceDto.</returns>
        Task<PaginatedResponse<DataSourceDto>> GetAllDataSourcesForProjectAsync(
            int projectId,
            string? filterOn = null, string? filterQuery = null, string? sortBy = null,
            bool isAscending = true, int pageNumber = 1, int pageSize = 25
        );

        /// <summary>
        /// Retrieves a specific data source by its ID.
        /// </summary>
        /// <param name="dataSourceId">The ID of the data source.</param>
        /// <returns>A task that represents the asynchronous operation, containing the DataSourceDto if found, otherwise null.</returns>
        Task<DataSourceDto?> GetDataSourceByIdAsync(int dataSourceId);

        /// <summary>
        /// Creates a new data source for a specific project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="createDto">The DTO containing the details of the data source to create.</param>
        /// <returns>A task that represents the asynchronous operation, containing the created DataSourceDto if successful, otherwise null.</returns>
        Task<DataSourceDto?> CreateDataSourceAsync(int projectId, CreateDataSourceDto createDto);

        /// <summary>
        /// Updates an existing data source.
        /// </summary>
        /// <param name="dataSourceId">The ID of the data source to update.</param>
        /// <param name="updateDto">The DTO containing the updated details of the data source.</param>
        /// <returns>A task that represents the asynchronous operation, containing the updated DataSourceDto if successful, otherwise null.</returns>
        Task<DataSourceDto?> UpdateDataSourceAsync(int dataSourceId, UpdateDataSourceDto updateDto);

        /// <summary>
        /// Deletes a data source by its ID.
        /// </summary>
        /// <param name="dataSourceId">The ID of the data source to delete.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if the data source was successfully deleted, otherwise false.</returns>
        Task<bool> DeleteDataSourceAsync(int dataSourceId);

        /// <summary>
        /// Gets all data source types that are currently configured and available for use.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing a collection of available DataSourceType.</returns>
        Task<IEnumerable<DataSourceType>> GetAvailableDataSourceTypesAsync();
    }
}