using System;
using server.Models.DTOs;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository projectRepository, ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    )
    {
        _logger.LogInformation("Fetching all projects.");
        var projects = await _projectRepository.GetAllAsync(
            filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize
        );
        _logger.LogInformation("Fetched {Count} projects.", projects.Count());
        if (projects == null || !projects.Any())
        {
            _logger.LogWarning("No projects found.");
            return [];
        }
        _logger.LogInformation("Mapping projects to DTOs.");

        return [.. projects.Select(p => new ProjectDto
        {
            Id = p.ProjectId,
            Name = p.Name,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            OwnerId = p.OwnerId,
            ProjectType = p.ProjectType,
            Status = p.Status
        })];
    }
}
