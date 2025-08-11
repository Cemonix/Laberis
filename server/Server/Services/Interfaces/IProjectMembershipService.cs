using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Services.Interfaces
{
    /// <summary>
    /// Service interface for centralized project membership validation and role checking.
    /// </summary>
    public interface IProjectMembershipService
    {
        /// <summary>
        /// Checks if a user is a member of the specified project.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="projectId">The project ID to check</param>
        /// <returns>True if user is a member, false otherwise</returns>
        Task<bool> IsProjectMemberAsync(string userId, int projectId);

        /// <summary>
        /// Gets the user's role in the specified project.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="projectId">The project ID to check</param>
        /// <returns>The user's role in the project, or null if not a member</returns>
        Task<ProjectRole?> GetUserRoleInProjectAsync(string userId, int projectId);

        /// <summary>
        /// Checks if a user has any of the specified roles in the project.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="projectId">The project ID to check</param>
        /// <param name="requiredRoles">The roles to check for</param>
        /// <returns>True if user has one of the required roles, false otherwise</returns>
        Task<bool> HasProjectRoleAsync(string userId, int projectId, params ProjectRole[] requiredRoles);

        /// <summary>
        /// Gets all projects that the user is a member of.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>List of project IDs the user is a member of</returns>
        Task<IEnumerable<int>> GetUserProjectsAsync(string userId);

        /// <summary>
        /// Gets the project membership details for a user.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="projectId">The project ID to check</param>
        /// <returns>ProjectMember entity if found, null otherwise</returns>
        Task<ProjectMember?> GetProjectMembershipAsync(string userId, int projectId);

        /// <summary>
        /// Validates if a user can perform a specific action based on project role requirements.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="projectId">The project ID to check</param>
        /// <param name="requiredRoles">The roles required for the action</param>
        /// <returns>True if user can perform the action, false otherwise</returns>
        Task<bool> CanUserPerformActionAsync(string userId, int projectId, params ProjectRole[] requiredRoles);
    }
}