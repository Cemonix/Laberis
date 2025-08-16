using server.Models.DTOs.Dashboard;

namespace server.Services.Interfaces;

/// <summary>
/// Service interface for dashboard analytics and metrics calculation
/// </summary>
public interface IDashboardAnalyticsService
{
    /// <summary>
    /// Get comprehensive dashboard analytics for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="dateFrom">Optional start date for analytics</param>
    /// <param name="dateTo">Optional end date for analytics</param>
    /// <returns>Complete dashboard analytics</returns>
    Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(int projectId, DateTime? dateFrom = null, DateTime? dateTo = null);

    /// <summary>
    /// Get workflow progress analytics for specific workflows or all workflows in a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="workflowId">Optional specific workflow ID</param>
    /// <returns>Workflow progress analytics</returns>
    Task<List<WorkflowProgressDto>> GetWorkflowProgressAsync(int projectId, int? workflowId = null);

    /// <summary>
    /// Get user performance analytics for project members
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">Optional specific user ID</param>
    /// <param name="dateFrom">Optional start date</param>
    /// <param name="dateTo">Optional end date</param>
    /// <returns>User performance analytics</returns>
    Task<List<UserPerformanceDto>> GetUserPerformanceAsync(int projectId, string? userId = null, DateTime? dateFrom = null, DateTime? dateTo = null);

    /// <summary>
    /// Get recent activity for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="limit">Number of activities to return</param>
    /// <returns>Recent activity list</returns>
    Task<List<RecentActivityDto>> GetRecentActivityAsync(int projectId, int limit = 50);

    /// <summary>
    /// Get project health overview
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Project health metrics</returns>
    Task<ProjectHealthDto> GetProjectHealthAsync(int projectId);

    /// <summary>
    /// Get task statistics for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="dateFrom">Optional start date</param>
    /// <param name="dateTo">Optional end date</param>
    /// <returns>Task statistics</returns>
    Task<TaskStatisticsDto> GetTaskStatisticsAsync(int projectId, DateTime? dateFrom = null, DateTime? dateTo = null);

    /// <summary>
    /// Get workflow stage performance analytics
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="workflowId">Workflow ID</param>
    /// <returns>Stage performance analytics</returns>
    Task<List<WorkflowStageProgressDto>> GetWorkflowStageProgressAsync(int projectId, int workflowId);
}