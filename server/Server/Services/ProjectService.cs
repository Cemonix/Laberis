using System.Linq.Expressions;
using System.Text.Json;
using server.Data;
using server.Models.Common;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.Project;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IStorageServiceFactory _storageServiceFactory;
    private readonly LaberisDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IProjectRepository projectRepository,
        IDataSourceRepository dataSourceRepository,
        IStorageServiceFactory storageServiceFactory,
        LaberisDbContext context,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
        _storageServiceFactory = storageServiceFactory ?? throw new ArgumentNullException(nameof(storageServiceFactory));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<ProjectDto>> GetAllProjectsAsync(
        string userId,
        string userRole,
        string? filterOn = null,
        string? filterQuery = null,
        string? sortBy = null,
        bool isAscending = true,
        int pageNumber = 1,
        int pageSize = 25
    )
    {
        _logger.LogInformation("Fetching projects for user {UserId} with role {UserRole}.", userId, userRole);

        Expression<Func<Project, bool>>? filter = null;

        if (userRole == Role.ADMIN.ToString())
        {
            // Admins see all projects, so the filter remains null
            _logger.LogInformation("User is Admin, fetching all projects.");
        }
        else if (userRole == Role.MANAGER.ToString())
        {
            // Managers see projects they own or are a member of
            filter = p => p.OwnerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId);
        }
        else
        {
            // Other roles see only projects they are a member of
            filter = p => p.ProjectMembers.Any(pm => pm.UserId == userId);
        }

        var (projects, totalCount) = await _projectRepository.GetAllWithCountAsync(
            filter: filter,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("User {UserId} has access to {Count} projects.", userId, totalCount);

        var projectDtos = projects.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PaginatedResponse<ProjectDto>
        {
            Data = projectDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int id)
    {
        _logger.LogInformation("Fetching project with ID: {ProjectId}", id);
        var project = await _projectRepository.GetByIdAsync(id);

        if (project == null)
        {
            _logger.LogWarning("Project with ID: {ProjectId} not found.", id);
            return null;
        }

        return MapToDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto createDto, string ownerId)
    {
        _logger.LogInformation("Attempting to create a new project with name: {ProjectName}", createDto.Name);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var project = new Project
            {
                Name = createDto.Name,
                Description = createDto.Description ?? string.Empty,
                ProjectType = createDto.ProjectType,
                OwnerId = ownerId,
                Status = ProjectStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} saved to database.", project.ProjectId);

            // Create the storage container via the factory
            // TODO: Type is hardcoded for now, but could be made configurable - based on the connection configuration
            const DataSourceType defaultSourceType = DataSourceType.MINIO_BUCKET;
            var storageService = _storageServiceFactory.GetService(defaultSourceType);
            string bucketName = storageService.GenerateBucketName(project.ProjectId);

            await storageService.CreateBucketAsync(bucketName);
            _logger.LogInformation("Default storage bucket created for project {ProjectId}.", project.ProjectId);

            // Create and save the default DataSource entity
            var dataSource = new DataSource
            {
                ProjectId = project.ProjectId,
                Name = "Default",
                Description = "Default Minio bucket for this project.",
                SourceType = defaultSourceType,
                ConnectionDetails = $"{{ \"BucketName\": \"{bucketName}\" }}",
                Status = DataSourceStatus.ACTIVE,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dataSourceRepository.AddAsync(dataSource);
            await _dataSourceRepository.SaveChangesAsync();
            _logger.LogInformation("Default data source for project {ProjectId} saved to database.", project.ProjectId);

            // If all steps succeed, commit the transaction
            await transaction.CommitAsync();
            _logger.LogInformation("Successfully created project {ProjectId} and its resources.", project.ProjectId);

            return MapToDto(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project. Rolling back transaction.");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ProjectDto?> UpdateProjectAsync(int id, UpdateProjectDto updateDto)
    {
        _logger.LogInformation("Attempting to update project with ID: {ProjectId}", id);
        var existingProject = await _projectRepository.GetByIdAsync(id);

        if (existingProject == null)
        {
            _logger.LogWarning("Update failed. Project with ID: {ProjectId} not found.", id);
            return null;
        }

        var updatedProject = existingProject with
        {
            Name = updateDto.Name,
            Description = updateDto.Description ?? existingProject.Description,
            Status = updateDto.Status,
            AnnotationGuidelinesUrl = updateDto.AnnotationGuidelinesUrl,
            UpdatedAt = DateTime.UtcNow
        };

        _projectRepository.Update(updatedProject);
        await _projectRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated project with ID: {ProjectId}", id);

        return MapToDto(updatedProject);
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        _logger.LogInformation("Attempting to soft-delete project with ID: {ProjectId}", id);

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            _logger.LogWarning("Soft delete failed. Project with ID: {ProjectId} not found.", id);
            return false;
        }

        // Instead of deleting, we change the project's status.
        var archivedProject = project with
        {
            Status = ProjectStatus.PENDING_DELETION,
            UpdatedAt = DateTime.UtcNow
        };

        _projectRepository.Update(archivedProject);
        await _projectRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully marked project {ProjectId} as PENDING_DELETION.", id);
        return true;
    }
    
    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.ProjectId,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            OwnerId = project.OwnerId,
            ProjectType = project.ProjectType,
            Status = project.Status,
            AnnotationGuidelinesUrl = project.AnnotationGuidelinesUrl
        };
    }
}