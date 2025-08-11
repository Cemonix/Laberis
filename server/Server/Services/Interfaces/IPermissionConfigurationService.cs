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
        /// Gets the complete permission configuration for the frontend.
        /// </summary>
        /// <returns>Permission configuration DTO</returns>
        PermissionConfigurationDto GetPermissionConfiguration();

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
        /// Reloads the permission configuration from the configuration file.
        /// </summary>
        void ReloadConfiguration();
    }
}