namespace server.Models.DTOs.Configuration
{
    /// <summary>
    /// DTO for permission configuration that is sent to the frontend.
    /// Contains permission definitions and role mappings.
    /// </summary>
    public class PermissionConfigurationDto
    {
        /// <summary>
        /// Hierarchical permission definitions grouped by category.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Permissions { get; set; } = [];

        /// <summary>
        /// Role-to-permissions mapping. Each role maps to a list of permission strings.
        /// </summary>
        public Dictionary<string, List<string>> RolePermissions { get; set; } = [];
    }

    /// <summary>
    /// Internal model for loading permission configuration from JSON.
    /// </summary>
    public class PermissionConfiguration
    {
        /// <summary>
        /// Role definitions with their permissions.
        /// </summary>
        public Dictionary<string, RoleDefinition> Roles { get; set; } = [];

        /// <summary>
        /// Global permissions available to all authenticated users.
        /// </summary>
        public List<string> GlobalPermissions { get; set; } = [];
    }

    /// <summary>
    /// Role definition containing name and permissions.
    /// </summary>
    public class RoleDefinition
    {
        /// <summary>
        /// Display name of the role.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of permissions granted to this role.
        /// </summary>
        public List<string> Permissions { get; set; } = [];
    }

    /// <summary>
    /// User permission context containing all applicable permissions for a user.
    /// </summary>
    public class UserPermissionContext
    {
        /// <summary>
        /// All permissions available to the user (role-based + global).
        /// </summary>
        public HashSet<string> Permissions { get; set; } = [];

        /// <summary>
        /// Project-specific role permissions grouped by project ID.
        /// </summary>
        public Dictionary<int, HashSet<string>> ProjectPermissions { get; set; } = [];

        /// <summary>
        /// Global permissions available regardless of project context.
        /// </summary>
        public HashSet<string> GlobalPermissions { get; set; } = [];
    }
}