using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Models.DTOs.DataSource;
using server.Models.Internal;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services
{
    public class DataSourceService : IDataSourceService
    {
        private readonly IDataSourceRepository _dataSourceRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IStorageServiceFactory _storageServiceFactory;
        private readonly ILogger<DataSourceService> _logger;

        public DataSourceService(
            IDataSourceRepository dataSourceRepository, 
            IProjectRepository projectRepository,
            IAssetRepository assetRepository,
            IStorageServiceFactory storageServiceFactory,
            ILogger<DataSourceService> logger)
        {
            _dataSourceRepository = dataSourceRepository;
            _projectRepository = projectRepository;
            _assetRepository = assetRepository;
            _storageServiceFactory = storageServiceFactory;
            _logger = logger;
        }

        public async Task<PaginatedResponse<DataSourceDto>> GetAllDataSourcesForProjectAsync(
            int projectId,
            string? filterOn = null, string? filterQuery = null, string? sortBy = null,
            bool isAscending = true, int pageNumber = 1, int pageSize = 25)
        {
            var (dataSources, totalCount) = await _dataSourceRepository.GetAllWithCountAsync(
                filter: ds => ds.ProjectId == projectId,
                filterOn: filterOn,
                filterQuery: filterQuery,
                sortBy: sortBy,
                isAscending: isAscending,
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var dataSourceDtos = new List<DataSourceDto>();
            foreach (var ds in dataSources)
            {
                // Get asset count for this data source
                var (_, assetCount) = await _assetRepository.GetAllWithCountAsync(
                    filter: asset => asset.DataSourceId == ds.DataSourceId,
                    pageSize: 1 // We only need the count, so minimal page size
                );

                dataSourceDtos.Add(new DataSourceDto
                {
                    Id = ds.DataSourceId,
                    Name = ds.Name,
                    Description = ds.Description,
                    SourceType = ds.SourceType,
                    Status = ds.Status,
                    IsDefault = ds.IsDefault,
                    CreatedAt = ds.CreatedAt,
                    ProjectId = ds.ProjectId,
                    AssetCount = assetCount
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResponse<DataSourceDto>
            {
                Data = [.. dataSourceDtos],
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalCount
            };
        }

        public async Task<DataSourceDto?> GetDataSourceByIdAsync(int dataSourceId)
        {
            var ds = await _dataSourceRepository.GetByIdAsync(dataSourceId);
            if (ds == null) return null;

            // Get asset count for this data source
            var (_, assetCount) = await _assetRepository.GetAllWithCountAsync(
                filter: asset => asset.DataSourceId == ds.DataSourceId,
                pageSize: 1 // We only need the count, so minimal page size
            );

            return new DataSourceDto
            {
                Id = ds.DataSourceId,
                Name = ds.Name,
                Description = ds.Description,
                SourceType = ds.SourceType,
                Status = ds.Status,
                IsDefault = ds.IsDefault,
                CreatedAt = ds.CreatedAt,
                ProjectId = ds.ProjectId,
                AssetCount = assetCount
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

            existingDataSource.Name = updateDto.Name ?? existingDataSource.Name;
            existingDataSource.Description = updateDto.Description ?? existingDataSource.Description;
            existingDataSource.Status = updateDto.Status;
            existingDataSource.ConnectionDetails = updateDto.ConnectionDetails ?? existingDataSource.ConnectionDetails;
            existingDataSource.UpdatedAt = DateTime.UtcNow;

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

        public async Task<IEnumerable<DataSourceType>> GetAvailableDataSourceTypesAsync()
        {
            var availableTypes = new List<DataSourceType>();
            
            // Check which storage services are actually configured
            foreach (var dataSourceType in Enum.GetValues<DataSourceType>())
            {
                try
                {
                    // Try to get the service - if it exists and is properly configured, include it
                    var service = _storageServiceFactory.GetService(dataSourceType);
                    if (service != null)
                    {
                        availableTypes.Add(dataSourceType);
                    }
                }
                catch (NotSupportedException)
                {
                    // Service not registered/configured for this type
                    _logger.LogDebug("Data source type {DataSourceType} is not configured", dataSourceType);
                }
                catch (Exception ex)
                {
                    // Log other exceptions but continue checking other types
                    _logger.LogWarning(ex, "Error checking availability of data source type {DataSourceType}", dataSourceType);
                }
            }
            
            return await System.Threading.Tasks.Task.FromResult(availableTypes);
        }

        public async Task<WorkflowDataSources> EnsureRequiredDataSourcesExistAsync(int projectId, bool includeReviewStage)
        {
            _logger.LogInformation("Ensuring required data sources exist for project {ProjectId}, includeReview: {IncludeReview}",
                projectId, includeReviewStage);

            // Get all existing data sources for the project
            var existingDataSources = await GetAllDataSourcesForProjectAsync(
                projectId, pageNumber: 1, pageSize: 100); // Get all data sources

            var result = new WorkflowDataSources
            {
                // Find or create annotation data source (look for default first)
                AnnotationDataSource = existingDataSources.Data.FirstOrDefault(ds => ds.IsDefault)
                    ?? existingDataSources.Data.FirstOrDefault()
            };

            if (result.AnnotationDataSource == null)
            {
                _logger.LogWarning("No data sources exist for project {ProjectId} - creating default annotation data source", projectId);

                result.AnnotationDataSource = await CreateDataSourceAsync(projectId, new CreateDataSourceDto
                {
                    Name = "Default Annotation Source",
                    Description = "Default data source for annotation assets",
                    SourceType = DataSourceType.MINIO_BUCKET
                });

                if (result.AnnotationDataSource == null)
                {
                    _logger.LogError("Failed to create annotation data source for project {ProjectId}", projectId);
                    throw new InvalidOperationException($"Failed to create required annotation data source for project {projectId}");
                }
            }

            // Create or find review data source (if review stage requested)
            if (includeReviewStage)
            {
                result.ReviewDataSource = existingDataSources.Data.FirstOrDefault(ds =>
                    ds.Name.Contains("review", StringComparison.CurrentCultureIgnoreCase)
                    || ds.Name.Contains("revision", StringComparison.CurrentCultureIgnoreCase));

                if (result.ReviewDataSource == null)
                {
                    _logger.LogInformation("Creating review data source for project {ProjectId}", projectId);

                    result.ReviewDataSource = await CreateDataSourceAsync(projectId, new CreateDataSourceDto
                    {
                        Name = "Review Stage Source",
                        Description = "Data source for assets in review stage",
                        SourceType = DataSourceType.MINIO_BUCKET
                    });

                    if (result.ReviewDataSource == null)
                    {
                        _logger.LogError("Failed to create review data source for project {ProjectId}", projectId);
                        throw new InvalidOperationException($"Failed to create required review data source for project {projectId}");
                    }
                }
            }

            // Create or find completion data source
            result.CompletionDataSource = existingDataSources.Data.FirstOrDefault(ds =>
                ds.Name.Contains("completion", StringComparison.CurrentCultureIgnoreCase)
                || ds.Name.Contains("final", StringComparison.CurrentCultureIgnoreCase)
                || ds.Name.Contains("complete", StringComparison.CurrentCultureIgnoreCase));

            if (result.CompletionDataSource == null)
            {
                _logger.LogInformation("Creating completion data source for project {ProjectId}", projectId);

                result.CompletionDataSource = await CreateDataSourceAsync(projectId, new CreateDataSourceDto
                {
                    Name = "Completion Stage Source",
                    Description = "Data source for completed and exported assets",
                    SourceType = DataSourceType.MINIO_BUCKET
                });

                if (result.CompletionDataSource == null)
                {
                    _logger.LogError("Failed to create completion data source for project {ProjectId}", projectId);
                    throw new InvalidOperationException($"Failed to create required completion data source for project {projectId}");
                }
            }

            _logger.LogInformation("Data sources configured - Annotation: {AnnotationId}, Review: {ReviewId}, Completion: {CompletionId}",
                result.AnnotationDataSource?.Id, result.ReviewDataSource?.Id, result.CompletionDataSource?.Id);

            return result;
        }
    }
}