using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Models.DTOs.Dashboard;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

/// <summary>
/// Controller for dashboard analytics and configuration
/// </summary>
[Route("api/projects/{projectId}/[controller]")]
[ApiController]
[Authorize]
[ProjectAccess]
[EnableRateLimiting("project")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardAnalyticsService _analyticsService;
    private readonly IDashboardConfigurationService _configurationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardAnalyticsService analyticsService,
        IDashboardConfigurationService configurationService,
        ILogger<DashboardController> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get comprehensive dashboard analytics for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="dateFrom">Optional start date for analytics (ISO format)</param>
    /// <param name="dateTo">Optional end date for analytics (ISO format)</param>
    /// <returns>Complete dashboard analytics</returns>
    [HttpGet("analytics")]
    public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboardAnalytics(
        int projectId,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(projectId, dateFrom, dateTo);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving dashboard analytics");
        }
    }

    /// <summary>
    /// Get workflow progress analytics
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="workflowId">Optional specific workflow ID</param>
    /// <returns>Workflow progress analytics</returns>
    [HttpGet("analytics/workflow-progress")]
    public async Task<ActionResult<List<WorkflowProgressDto>>> GetWorkflowProgress(
        int projectId,
        [FromQuery] int? workflowId = null)
    {
        try
        {
            var progress = await _analyticsService.GetWorkflowProgressAsync(projectId, workflowId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow progress for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving workflow progress");
        }
    }

    /// <summary>
    /// Get user performance analytics
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">Optional specific user ID</param>
    /// <param name="dateFrom">Optional start date</param>
    /// <param name="dateTo">Optional end date</param>
    /// <returns>User performance analytics</returns>
    [HttpGet("analytics/user-performance")]
    public async Task<ActionResult<List<UserPerformanceDto>>> GetUserPerformance(
        int projectId,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var performance = await _analyticsService.GetUserPerformanceAsync(projectId, userId, dateFrom, dateTo);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user performance for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving user performance");
        }
    }

    /// <summary>
    /// Get recent activity for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="limit">Number of activities to return (max 100)</param>
    /// <returns>Recent activity list</returns>
    [HttpGet("analytics/recent-activity")]
    public async Task<ActionResult<List<RecentActivityDto>>> GetRecentActivity(
        int projectId,
        [FromQuery] int limit = 50)
    {
        try
        {
            var limitCapped = Math.Min(100, Math.Max(1, limit));
            var activities = await _analyticsService.GetRecentActivityAsync(projectId, limitCapped);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activity for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving recent activity");
        }
    }

    /// <summary>
    /// Get project health overview
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Project health metrics</returns>
    [HttpGet("analytics/project-health")]
    public async Task<ActionResult<ProjectHealthDto>> GetProjectHealth(int projectId)
    {
        try
        {
            var health = await _analyticsService.GetProjectHealthAsync(projectId);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project health for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving project health");
        }
    }

    /// <summary>
    /// Get task statistics for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="dateFrom">Optional start date</param>
    /// <param name="dateTo">Optional end date</param>
    /// <returns>Task statistics</returns>
    [HttpGet("analytics/task-statistics")]
    public async Task<ActionResult<TaskStatisticsDto>> GetTaskStatistics(
        int projectId,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var statistics = await _analyticsService.GetTaskStatisticsAsync(projectId, dateFrom, dateTo);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task statistics for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving task statistics");
        }
    }

    /// <summary>
    /// Get workflow stage progress analytics
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="workflowId">Workflow ID</param>
    /// <returns>Stage progress analytics</returns>
    [HttpGet("analytics/workflow-stage-progress")]
    public async Task<ActionResult<List<WorkflowStageProgressDto>>> GetWorkflowStageProgress(
        int projectId,
        [FromQuery] int workflowId)
    {
        try
        {
            var progress = await _analyticsService.GetWorkflowStageProgressAsync(projectId, workflowId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow stage progress for project {ProjectId}, workflow {WorkflowId}", projectId, workflowId);
            return StatusCode(500, "An error occurred while retrieving workflow stage progress");
        }
    }

    /// <summary>
    /// Get user's dashboard configuration
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Dashboard configuration</returns>
    [HttpGet("configuration")]
    public async Task<ActionResult<DashboardConfigurationDto>> GetDashboardConfiguration(int projectId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var configuration = await _configurationService.GetDashboardConfigurationAsync(projectId, userId);
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard configuration for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving dashboard configuration");
        }
    }

    /// <summary>
    /// Create or update user's dashboard configuration
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="configurationDto">Dashboard configuration data</param>
    /// <returns>Updated dashboard configuration</returns>
    [HttpPut("configuration")]
    public async Task<ActionResult<DashboardConfigurationDto>> UpdateDashboardConfiguration(
        int projectId,
        [FromBody] CreateUpdateDashboardConfigurationDto configurationDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }

            var configuration = await _configurationService.CreateOrUpdateDashboardConfigurationAsync(
                projectId, userId, configurationDto);
            
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard configuration for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while updating dashboard configuration");
        }
    }

    /// <summary>
    /// Get available widget definitions
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Available widget definitions</returns>
    [HttpGet("widgets/definitions")]
    public async Task<ActionResult<List<WidgetDefinitionDto>>> GetWidgetDefinitions(int projectId)
    {
        try
        {
            var definitions = await _configurationService.GetAvailableWidgetDefinitionsAsync(projectId);
            return Ok(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget definitions for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving widget definitions");
        }
    }

    /// <summary>
    /// Get widget data with filters
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Widget data request</param>
    /// <returns>Widget data</returns>
    [HttpPost("widgets/data")]
    public async Task<ActionResult<WidgetDataDto>> GetWidgetData(
        int projectId,
        [FromBody] WidgetDataRequestDto request)
    {
        try
        {
            var widgetData = await _configurationService.GetWidgetDataAsync(projectId, request);
            return Ok(widgetData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget data for project {ProjectId}, widget type {WidgetType}", 
                projectId, request.WidgetType);
            return StatusCode(500, "An error occurred while retrieving widget data");
        }
    }
}