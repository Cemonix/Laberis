using server.Models.DTOs;
using server.Models.DTOs.DataSource;

namespace server.Services.Interfaces
{
    public interface IDataSourceService
    {
        Task<IEnumerable<DataSourceDto>> GetAllDataSourcesForProjectAsync(int projectId);
        Task<DataSourceDto?> GetDataSourceByIdAsync(int dataSourceId);
        Task<DataSourceDto?> CreateDataSourceAsync(int projectId, CreateDataSourceDto createDto);
        Task<DataSourceDto?> UpdateDataSourceAsync(int dataSourceId, UpdateDataSourceDto updateDto);
        Task<bool> DeleteDataSourceAsync(int dataSourceId);
    }
}