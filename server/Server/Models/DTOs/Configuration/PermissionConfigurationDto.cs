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
        public Dictionary<string, Dictionary<string, string>> Permissions { get; set; } = [];
        public Dictionary<string, List<string>> RolePermissions { get; set; } = [];
    }
}