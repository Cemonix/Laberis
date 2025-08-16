using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Models.DTOs.Dashboard;
using server.Services.Interfaces;

namespace server.Services;

/// <summary>
/// Service for dashboard configuration management
/// </summary>
public class DashboardConfigurationService : IDashboardConfigurationService
{
    private readonly LaberisDbContext _context;
    private readonly IDashboardAnalyticsService _analyticsService;

    public DashboardConfigurationService(
        LaberisDbContext context,
        IDashboardAnalyticsService analyticsService)
    {
        _context = context;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get user's dashboard configuration for a project
    /// </summary>
    public async Task<DashboardConfigurationDto> GetDashboardConfigurationAsync(int projectId, string userId)
    {
        var configuration = await _context.DashboardConfigurations
            .FirstOrDefaultAsync(dc => dc.ProjectId == projectId && dc.UserId == userId);

        if (configuration == null)
        {
            // Return default configuration
            return new DashboardConfigurationDto
            {
                DashboardConfigurationId = 0,
                ProjectId = projectId,
                UserId = userId,
                ConfigurationData = GetDefaultConfiguration(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        return new DashboardConfigurationDto
        {
            DashboardConfigurationId = configuration.DashboardConfigurationId,
            ProjectId = configuration.ProjectId,
            UserId = configuration.UserId,
            ConfigurationData = configuration.ConfigurationData,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }

    /// <summary>
    /// Create or update user's dashboard configuration
    /// </summary>
    public async Task<DashboardConfigurationDto> CreateOrUpdateDashboardConfigurationAsync(
        int projectId, 
        string userId, 
        CreateUpdateDashboardConfigurationDto configurationDto)
    {
        var existingConfiguration = await _context.DashboardConfigurations
            .FirstOrDefaultAsync(dc => dc.ProjectId == projectId && dc.UserId == userId);

        if (existingConfiguration == null)
        {
            // Create new configuration
            var newConfiguration = new DashboardConfiguration
            {
                ProjectId = projectId,
                UserId = userId,
                ConfigurationData = configurationDto.ConfigurationData,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DashboardConfigurations.Add(newConfiguration);
            await _context.SaveChangesAsync();

            return new DashboardConfigurationDto
            {
                DashboardConfigurationId = newConfiguration.DashboardConfigurationId,
                ProjectId = newConfiguration.ProjectId,
                UserId = newConfiguration.UserId,
                ConfigurationData = newConfiguration.ConfigurationData,
                CreatedAt = newConfiguration.CreatedAt,
                UpdatedAt = newConfiguration.UpdatedAt
            };
        }
        else
        {
            // Update existing configuration
            existingConfiguration.ConfigurationData = configurationDto.ConfigurationData;
            existingConfiguration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new DashboardConfigurationDto
            {
                DashboardConfigurationId = existingConfiguration.DashboardConfigurationId,
                ProjectId = existingConfiguration.ProjectId,
                UserId = existingConfiguration.UserId,
                ConfigurationData = existingConfiguration.ConfigurationData,
                CreatedAt = existingConfiguration.CreatedAt,
                UpdatedAt = existingConfiguration.UpdatedAt
            };
        }
    }

    /// <summary>
    /// Get available widget definitions for a project based on user role
    /// </summary>
    public async Task<List<WidgetDefinitionDto>> GetAvailableWidgetDefinitionsAsync(int projectId)
    {
        // For now, return all widget definitions. In future, filter by user role
        var widgetDefinitions = new List<WidgetDefinitionDto>
        {
            new()
            {
                WidgetType = "workflow_progress",
                Title = "Workflow Progress",
                Description = "Visual progress tracking for workflow stages and task completion",
                DefaultWidth = 8,
                DefaultHeight = 6,
                RequiresConfiguration = true,
                SupportedRoles = ["VIEWER", "ANNOTATOR", "REVIEWER", "MANAGER"],
                AvailableSettings =
                [
                    new WidgetSettingDto
                    {
                        Key = "workflowId",
                        Label = "Workflow",
                        Type = "select",
                        Required = false,
                        HelpText = "Select specific workflow or leave empty for all workflows"
                    },
                    new WidgetSettingDto
                    {
                        Key = "showStageDetails",
                        Label = "Show Stage Details",
                        Type = "boolean",
                        DefaultValue = true,
                        HelpText = "Display detailed stage progress information"
                    }
                ]
            },
            new()
            {
                WidgetType = "user_performance",
                Title = "Team Performance",
                Description = "User productivity metrics and performance analytics",
                DefaultWidth = 6,
                DefaultHeight = 8,
                RequiresConfiguration = false,
                SupportedRoles = ["REVIEWER", "MANAGER"],
                AvailableSettings =
                [
                    new WidgetSettingDto
                    {
                        Key = "dateRange",
                        Label = "Date Range",
                        Type = "select",
                        DefaultValue = "30",
                        Options =
                        [
                            new WidgetSettingOptionDto { Value = "7", Label = "Last 7 days" },
                            new WidgetSettingOptionDto { Value = "30", Label = "Last 30 days" },
                            new WidgetSettingOptionDto { Value = "90", Label = "Last 90 days" }
                        ]
                    }
                ]
            },
            new()
            {
                WidgetType = "recent_activity",
                Title = "Recent Activity",
                Description = "Live feed of recent project activities and updates",
                DefaultWidth = 4,
                DefaultHeight = 6,
                RequiresConfiguration = false,
                SupportedRoles = ["VIEWER", "ANNOTATOR", "REVIEWER", "MANAGER"],
                AvailableSettings =
                [
                    new WidgetSettingDto
                    {
                        Key = "limit",
                        Label = "Number of Activities",
                        Type = "number",
                        DefaultValue = 20,
                        HelpText = "Maximum number of activities to display"
                    }
                ]
            },
            new()
            {
                WidgetType = "project_health",
                Title = "Project Health",
                Description = "High-level project status and health indicators",
                DefaultWidth = 6,
                DefaultHeight = 4,
                RequiresConfiguration = false,
                SupportedRoles = ["MANAGER"],
                AvailableSettings = []
            },
            new()
            {
                WidgetType = "task_statistics",
                Title = "Task Statistics",
                Description = "Detailed task metrics and completion statistics",
                DefaultWidth = 8,
                DefaultHeight = 5,
                RequiresConfiguration = false,
                SupportedRoles = ["REVIEWER", "MANAGER"],
                AvailableSettings =
                [
                    new WidgetSettingDto
                    {
                        Key = "chartType",
                        Label = "Chart Type",
                        Type = "select",
                        DefaultValue = "bar",
                        Options =
                        [
                            new WidgetSettingOptionDto { Value = "bar", Label = "Bar Chart" },
                            new WidgetSettingOptionDto { Value = "line", Label = "Line Chart" },
                            new WidgetSettingOptionDto { Value = "pie", Label = "Pie Chart" }
                        ]
                    }
                ]
            },
            new()
            {
                WidgetType = "workflow_stage_progress",
                Title = "Stage Progress",
                Description = "Detailed progress analytics for specific workflow stages",
                DefaultWidth = 6,
                DefaultHeight = 6,
                RequiresConfiguration = true,
                SupportedRoles = ["ANNOTATOR", "REVIEWER", "MANAGER"],
                AvailableSettings =
                [
                    new WidgetSettingDto
                    {
                        Key = "workflowId",
                        Label = "Workflow",
                        Type = "select",
                        Required = true,
                        HelpText = "Select the workflow to display stage progress for"
                    }
                ]
            }
        };

        return await System.Threading.Tasks.Task.FromResult(widgetDefinitions);
    }

    /// <summary>
    /// Get widget data based on request parameters
    /// </summary>
    public async Task<WidgetDataDto> GetWidgetDataAsync(int projectId, WidgetDataRequestDto request)
    {
        object data = request.WidgetType switch
        {
            "workflow_progress" => await _analyticsService.GetWorkflowProgressAsync(
                projectId, request.WorkflowId),
            
            "user_performance" => await _analyticsService.GetUserPerformanceAsync(
                projectId, request.UserId, request.DateFrom, request.DateTo),
            
            "recent_activity" => await _analyticsService.GetRecentActivityAsync(
                projectId, request.Limit ?? 20),
                
            "project_health" => await _analyticsService.GetProjectHealthAsync(projectId),
            
            "task_statistics" => await _analyticsService.GetTaskStatisticsAsync(
                projectId, request.DateFrom, request.DateTo),
            
            "workflow_stage_progress" => await GetWorkflowStageProgressDataAsync(projectId, request.WorkflowId),
            
            _ => throw new ArgumentException($"Unknown widget type: {request.WidgetType}")
        };

        return new WidgetDataDto
        {
            WidgetType = request.WidgetType,
            Data = data,
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), // Cache for 5 minutes
            Metadata = new Dictionary<string, object>
            {
                ["projectId"] = projectId,
                ["requestFilters"] = request.CustomFilters
            }
        };
    }

    /// <summary>
    /// Delete user's dashboard configuration
    /// </summary>
    public async Task<bool> DeleteDashboardConfigurationAsync(int projectId, string userId)
    {
        var configuration = await _context.DashboardConfigurations
            .FirstOrDefaultAsync(dc => dc.ProjectId == projectId && dc.UserId == userId);

        if (configuration == null)
        {
            return false;
        }

        _context.DashboardConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();
        
        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Get workflow stage progress data with automatic workflow selection if not specified
    /// </summary>
    private async Task<List<WorkflowStageProgressDto>> GetWorkflowStageProgressDataAsync(int projectId, int? workflowId)
    {
        if (workflowId.HasValue)
        {
            return await _analyticsService.GetWorkflowStageProgressAsync(projectId, workflowId.Value);
        }

        // If no workflow ID specified, get the first available workflow for the project
        var firstWorkflow = await _context.Workflows
            .Where(w => w.ProjectId == projectId)
            .Select(w => w.WorkflowId)
            .FirstOrDefaultAsync();

        if (firstWorkflow == 0)
        {
            throw new InvalidOperationException($"No workflows found for project {projectId}. A workflow is required for stage progress widget.");
        }

        return await _analyticsService.GetWorkflowStageProgressAsync(projectId, firstWorkflow);
    }

    /// <summary>
    /// Get default dashboard configuration JSON
    /// </summary>
    private static string GetDefaultConfiguration()
    {
        var defaultLayout = new DashboardLayoutDto
        {
            Widgets =
            [
                new WidgetInstanceDto
                {
                    WidgetId = "project_health_1",
                    WidgetType = "project_health",
                    Title = "Project Health",
                    GridX = 0,
                    GridY = 0,
                    GridWidth = 6,
                    GridHeight = 4,
                    IsVisible = true,
                    Settings = new Dictionary<string, object>()
                },
                new WidgetInstanceDto
                {
                    WidgetId = "recent_activity_1",
                    WidgetType = "recent_activity",
                    Title = "Recent Activity",
                    GridX = 6,
                    GridY = 0,
                    GridWidth = 6,
                    GridHeight = 4,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["limit"] = 10 }
                },
                new WidgetInstanceDto
                {
                    WidgetId = "workflow_progress_1",
                    WidgetType = "workflow_progress",
                    Title = "Workflow Progress",
                    GridX = 0,
                    GridY = 4,
                    GridWidth = 8,
                    GridHeight = 6,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["showStageDetails"] = true }
                },
                new WidgetInstanceDto
                {
                    WidgetId = "task_statistics_1",
                    WidgetType = "task_statistics", 
                    Title = "Task Statistics",
                    GridX = 8,
                    GridY = 4,
                    GridWidth = 4,
                    GridHeight = 6,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["chartType"] = "bar" }
                }
            ],
            Theme = "light",
            RefreshIntervalSeconds = 300,
            LastModified = DateTime.UtcNow
        };

        return System.Text.Json.JsonSerializer.Serialize(defaultLayout);
    }

    #endregion
}