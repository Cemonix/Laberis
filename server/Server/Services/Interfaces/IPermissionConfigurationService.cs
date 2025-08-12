using server.Models.Domain.Enums;
using server.Models.DTOs.Configuration;

namespace server.Services.Interfaces
{
    /// <summary>
    /// Service for managing project permission configuration.
    /// Loads permission rules from configuration and provides them to frontend and backend.
    /// </summary>
    public interface IPermissionConfigurationService
    {
        /// <summary>
        /// Checks if a role has a specific permission based on the loaded configuration.
        /// </summary>
        /// <param name="role">The project role to check</param>
        /// <param name="permission">The permission to check for</param>
        /// <returns>True if the role has the permission, false otherwise</returns>
        bool HasPermission(ProjectRole role, string permission);

        /// <summary>
        /// Gets all permissions for a specific role based on the loaded configuration.
        /// </summary>
        /// <param name="role">The project role</param>
        /// <returns>Set of permissions for the role</returns>
        HashSet<string> GetPermissionsForRole(ProjectRole role);

        /// <summary>
        /// Gets global permissions that apply to all authenticated users.
        /// </summary>
        /// <returns>HashSet of global permission strings</returns>
        HashSet<string> GetGlobalPermissions();

        /// <summary>
        /// Builds a comprehensive permission context for a user based on their project memberships.
        /// </summary>
        /// <param name="userId">The user ID to build context for</param>
        /// <returns>UserPermissionContext containing all applicable permissions</returns>
        Task<UserPermissionContext> BuildUserPermissionContextAsync(string userId);

        /// <summary>
        /// Gets permissions for a specific page/route for a user.
        /// Used for hybrid mode where frontend requests permissions on-demand.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="page">The page/route identifier</param>
        /// <param name="projectId">Optional project ID for project-specific permissions</param>
        /// <returns>HashSet of permissions for the requested page</returns>
        Task<HashSet<string>> GetPagePermissionsAsync(string userId, string page, int? projectId = null);

        /// <summary>
        /// Reloads the permission configuration from the configuration file.
        /// </summary>
        void ReloadConfiguration();
    }
}