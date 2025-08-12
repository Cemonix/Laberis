namespace server.Services.Interfaces
{
    /// <summary>
    /// Service for managing page-specific user permissions and capabilities.
    /// Provides per-page permission resolution based on user's project role.
    /// </summary>
    public interface IPagePermissionService
    {
        /// <summary>
        /// Gets page-specific permissions for a user based on their project role.
        /// Returns capabilities and UI flags specific to the requested route.
        /// </summary>
        /// <param name="userId">The ID of the user requesting permissions</param>
        /// <param name="projectId">The ID of the project to check permissions for</param>
        /// <param name="route">The route/page to get permissions for (e.g., /projects/{id}/tasks)</param>
        /// <returns>Page-specific user capabilities, or null if route not found</returns>
        Task<object?> GetPagePermissionsAsync(string userId, int projectId, string route);

        /// <summary>
        /// Normalizes a route path by replacing project ID placeholders with generic tokens.
        /// Used to match dynamic routes against configuration templates.
        /// </summary>
        /// <param name="route">The route path to normalize</param>
        /// <param name="projectId">The project ID to replace with placeholder</param>
        /// <returns>Normalized route path</returns>
        string NormalizeRoute(string route, int projectId);
    }
}