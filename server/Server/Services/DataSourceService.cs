using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs;
using server.Models.DTOs.DataSource;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services
{
    public class DataSourceService : IDataSourceService
    {
        private readonly IDataSourceRepository _dataSourceRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<DataSourceService> _logger;

        public DataSourceService(IDataSourceRepository dataSourceRepository, IProjectRepository projectRepository, ILogger<DataSourceService> logger)
        {
            _dataSourceRepository = dataSourceRepository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<DataSourceDto>> GetAllDataSourcesForProjectAsync(int projectId)
        {
            var dataSources = await _dataSourceRepository.FindAsync(ds => ds.ProjectId == projectId);
            return dataSources.Select(ds => new DataSourceDto
            {
                Id = ds.DataSourceId,
                Name = ds.Name,
                Description = ds.Description,
                SourceType = ds.SourceType,
                Status = ds.Status,
                IsDefault = ds.IsDefault,
                CreatedAt = ds.CreatedAt,
                ProjectId = ds.ProjectId
            });
        }

        public async Task<DataSourceDto?> GetDataSourceByIdAsync(int dataSourceId)
        {
            var ds = await _dataSourceRepository.GetByIdAsync(dataSourceId);
            if (ds == null) return null;

            return new DataSourceDto
            {
                Id = ds.DataSourceId,
                Name = ds.Name,
                Description = ds.Description,
                SourceType = ds.SourceType,
                Status = ds.Status,
                IsDefault = ds.IsDefault,
                CreatedAt = ds.CreatedAt,
                ProjectId = ds.ProjectId
            };
        }

        public async Task<DataSourceDto?> CreateDataSourceAsync(int projectId, CreateDataSourceDto createDto)
        {
            var projectExists = await _projectRepository.GetByIdAsync(projectId) != null;
            if (!projectExists)
            {
                _logger.LogWarning("Attempted to create a data source for a non-existent project with ID: {ProjectId}", projectId);
                return null;
            }

            var newDataSource = new DataSource
            {
                Name = createDto.Name,
                Description = createDto.Description,
                SourceType = createDto.SourceType,
                ConnectionDetails = createDto.ConnectionDetails,
                Status = DataSourceStatus.ACTIVE,
                ProjectId = projectId
            };

            await _dataSourceRepository.AddAsync(newDataSource);
            await _dataSourceRepository.SaveChangesAsync();

            return await GetDataSourceByIdAsync(newDataSource.DataSourceId);
        }

        public async Task<DataSourceDto?> UpdateDataSourceAsync(int dataSourceId, UpdateDataSourceDto updateDto)
        {
            var existingDataSource = await _dataSourceRepository.GetByIdAsync(dataSourceId);
            if (existingDataSource == null) return null;

            var updatedDataSource = existingDataSource with
            {
                Name = updateDto.Name,
                Description = updateDto.Description,
                Status = updateDto.Status,
                ConnectionDetails = updateDto.ConnectionDetails,
                UpdatedAt = DateTime.UtcNow
            };
            
            _dataSourceRepository.Update(updatedDataSource);
            await _dataSourceRepository.SaveChangesAsync();

            return await GetDataSourceByIdAsync(dataSourceId);
        }

        public async Task<bool> DeleteDataSourceAsync(int dataSourceId)
        {
            var dataSource = await _dataSourceRepository.GetByIdAsync(dataSourceId);
            if (dataSource == null) return false;

            _dataSourceRepository.Remove(dataSource);
            await _dataSourceRepository.SaveChangesAsync();
            return true;
        }
    }
}