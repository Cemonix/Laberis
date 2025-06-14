using System;
using server.Models.DTOs;
using server.Models.DTOs.Project;

namespace server.Services.Interfaces;

public interface IProjectService
{   
    /// <summary>
    /// Retrieves all projects, optionally filtered and sorted, with pagination information.
    /// </summary>
    /// <param name="filterOn">The field to filter on (e.g., "name", "owner").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "name", "createdDate").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a paginated response of ProjectDto.</returns>
    Task<PaginatedResponse<ProjectDto>> GetAllProjectsAsync(
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a project by its ID.
    /// </summary>
    /// <param name="id">The ID of the project to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the ProjectDto if found, otherwise null.</returns>
    Task<ProjectDto?> GetProjectByIdAsync(int id);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The ID of the project to update.</param>
    /// <param name="updateDto">The DTO containing updated project information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated ProjectDto if successful, otherwise null.</returns>
    Task<ProjectDto?> UpdateProjectAsync(int id, UpdateProjectDto updateDto);

    /// <summary>
    /// Deletes a project by its ID.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the project was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteProjectAsync(int id);

    /// <summary>
    /// Creates a new project, including a default data source and storage container.
    /// </summary>
    /// <param name="projectDto">The DTO containing information for the new project.</param>
    /// <param name="ownerId">The ID of the user creating the project.</param>
    /// <returns>A DTO representing the newly created project.</returns>
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto projectDto, string ownerId);
}
