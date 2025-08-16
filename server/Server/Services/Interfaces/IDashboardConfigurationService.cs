using server.Models.DTOs.Dashboard;

namespace server.Services.Interfaces;

/// <summary>
/// Service interface for dashboard configuration management
/// </summary>
public interface IDashboardConfigurationService
{
    /// <summary>
    /// Get user's dashboard configuration for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Dashboard configuration or default if not exists</returns>
    Task<DashboardConfigurationDto> GetDashboardConfigurationAsync(int projectId, string userId);

    /// <summary>
    /// Create or update user's dashboard configuration
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="configurationDto">Configuration data</param>
    /// <returns>Updated dashboard configuration</returns>
    Task<DashboardConfigurationDto> CreateOrUpdateDashboardConfigurationAsync(int projectId, string userId, CreateUpdateDashboardConfigurationDto configurationDto);

    /// <summary>
    /// Get available widget definitions for a project based on user role
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Available widget definitions</returns>
    Task<List<WidgetDefinitionDto>> GetAvailableWidgetDefinitionsAsync(int projectId);

    /// <summary>
    /// Get widget data based on request parameters
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Widget data request</param>
    /// <returns>Widget data</returns>
    Task<WidgetDataDto> GetWidgetDataAsync(int projectId, WidgetDataRequestDto request);

    /// <summary>
    /// Delete user's dashboard configuration
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Success indicator</returns>
    Task<bool> DeleteDashboardConfigurationAsync(int projectId, string userId);
}