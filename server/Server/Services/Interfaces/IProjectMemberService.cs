using server.Models.Common;
using server.Models.DTOs.ProjectMember;

namespace server.Services.Interfaces;

public interface IProjectMemberService
{
    /// <summary>
    /// Retrieves all members for a specific project, optionally filtered and sorted.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve members for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "role", "user_id").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "role", "joined_at", "invited_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of ProjectMemberDto.</returns>
    Task<PaginatedResponse<ProjectMemberDto>> GetProjectMembersAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves all projects a user is a member of, optionally filtered and sorted.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve project memberships for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "role").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "role", "joined_at", "invited_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of ProjectMemberDto.</returns>
    Task<PaginatedResponse<ProjectMemberDto>> GetUserProjectMembershipsAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a project member by project and user ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the ProjectMemberDto if found, otherwise null.</returns>
    Task<ProjectMemberDto?> GetProjectMemberAsync(int projectId, string userId);

    /// <summary>
    /// Adds a new member to a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to add the member to.</param>
    /// <param name="createDto">The DTO containing information for the new project member.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created ProjectMemberDto.</returns>
    Task<ProjectMemberDto> AddProjectMemberAsync(int projectId, CreateProjectMemberDto createDto);

    /// <summary>
    /// Updates an existing project member's role.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="updateDto">The DTO containing updated project member information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated ProjectMemberDto if successful, otherwise null.</returns>
    Task<ProjectMemberDto?> UpdateProjectMemberAsync(int projectId, string userId, UpdateProjectMemberDto updateDto);

    /// <summary>
    /// Removes a member from a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="userId">The ID of the user to remove.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the member was successfully removed, otherwise false.</returns>
    Task<bool> RemoveProjectMemberAsync(int projectId, string userId);
}
