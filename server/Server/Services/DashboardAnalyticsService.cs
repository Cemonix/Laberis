using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.DTOs.Dashboard;
using server.Models.Domain.Enums;
using server.Services.Interfaces;
using Task = server.Models.Domain.Task;

namespace server.Services;

/// <summary>
/// Service for calculating dashboard analytics and metrics
/// </summary>
public class DashboardAnalyticsService : IDashboardAnalyticsService
{
    private readonly LaberisDbContext _context;

    public DashboardAnalyticsService(LaberisDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get comprehensive dashboard analytics for a project
    /// </summary>
    public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(int projectId, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        // Set default date range if not provided (last 30 days)
        dateFrom ??= DateTime.UtcNow.AddDays(-30);
        dateTo ??= DateTime.UtcNow;

        var projectHealth = await GetProjectHealthAsync(projectId);
        var workflowProgress = await GetWorkflowProgressAsync(projectId);
        var userPerformance = await GetUserPerformanceAsync(projectId, null, dateFrom, dateTo);
        var recentActivities = await GetRecentActivityAsync(projectId, 20);
        var taskStatistics = await GetTaskStatisticsAsync(projectId, dateFrom, dateTo);

        return new DashboardAnalyticsDto
        {
            ProjectHealth = projectHealth,
            WorkflowProgress = workflowProgress,
            UserPerformance = userPerformance,
            RecentActivities = recentActivities,
            TaskStatistics = taskStatistics,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get workflow progress analytics
    /// </summary>
    public async Task<List<WorkflowProgressDto>> GetWorkflowProgressAsync(int projectId, int? workflowId = null)
    {
        var workflowsQuery = _context.Workflows
            .Where(w => w.ProjectId == projectId);

        if (workflowId.HasValue)
        {
            workflowsQuery = workflowsQuery.Where(w => w.WorkflowId == workflowId.Value);
        }

        var workflows = await workflowsQuery
            .Include(w => w.Tasks)
            .Include(w => w.WorkflowStages)
            .ToListAsync();

        var workflowProgress = new List<WorkflowProgressDto>();

        foreach (var workflow in workflows)
        {
            var tasks = workflow.Tasks.ToList();
            var completedTasks = tasks.Count(t => t.CompletedAt.HasValue);
            var inProgressTasks = tasks.Count(t => !t.CompletedAt.HasValue && !t.SuspendedAt.HasValue && !t.DeferredAt.HasValue && t.AssignedToUserId != null);
            var notStartedTasks = tasks.Count(t => !t.CompletedAt.HasValue && !t.SuspendedAt.HasValue && !t.DeferredAt.HasValue && t.AssignedToUserId == null);
            var suspendedTasks = tasks.Count(t => t.SuspendedAt.HasValue);
            var deferredTasks = tasks.Count(t => t.DeferredAt.HasValue);

            var stageProgress = await GetWorkflowStageProgressAsync(projectId, workflow.WorkflowId);

            workflowProgress.Add(new WorkflowProgressDto
            {
                WorkflowId = workflow.WorkflowId,
                WorkflowName = workflow.Name,
                TotalTasks = tasks.Count,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                NotStartedTasks = notStartedTasks,
                SuspendedTasks = suspendedTasks,
                DeferredTasks = deferredTasks,
                CompletionPercentage = tasks.Count > 0 ? (decimal)completedTasks / tasks.Count * 100 : 0,
                StageProgress = stageProgress
            });
        }

        return workflowProgress;
    }

    /// <summary>
    /// Get user performance analytics
    /// </summary>
    public async Task<List<UserPerformanceDto>> GetUserPerformanceAsync(int projectId, string? userId = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        dateFrom ??= DateTime.UtcNow.AddDays(-30);
        dateTo ??= DateTime.UtcNow;

        var membersQuery = _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId);

        if (!string.IsNullOrEmpty(userId))
        {
            membersQuery = membersQuery.Where(pm => pm.UserId == userId);
        }

        var members = await membersQuery
            .Include(pm => pm.User)
            .ToListAsync();
        var userPerformance = new List<UserPerformanceDto>();

        foreach (var member in members)
        {
            var userTasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId && 
                           (t.AssignedToUserId == member.UserId || t.LastWorkedOnByUserId == member.UserId))
                .Include(t => t.TaskEvents.Where(te => te.CreatedAt >= dateFrom && te.CreatedAt <= dateTo))
                .ToListAsync();

            var assignedTasks = userTasks.Where(t => t.AssignedToUserId == member.UserId).ToList();
            var completedTasks = assignedTasks.Where(t => t.CompletedAt.HasValue && t.CompletedAt >= dateFrom && t.CompletedAt <= dateTo).ToList();
            var inProgressTasks = assignedTasks.Where(t => !t.CompletedAt.HasValue && t.AssignedToUserId == member.UserId).ToList();

            // Calculate veto rate (tasks returned for rework)
            var vetoedTasks = await _context.TaskEvents
                .Where(te => te.Task!.ProjectId == projectId && 
                    te.UserId == member.UserId &&
                    te.EventType == TaskEventType.STATUS_CHANGED &&
                    te.Details != null && te.Details.Contains("CHANGES_REQUIRED") &&
                    te.CreatedAt >= dateFrom && te.CreatedAt <= dateTo)
                .CountAsync();

            // Calculate average completion time
            var averageCompletionTime = CalculateAverageCompletionTime(completedTasks);

            // Calculate daily productivity
            var dailyProductivity = await CalculateDailyProductivity(member.UserId, projectId, dateFrom.Value, dateTo.Value);

            // Get last activity
            var lastActivity = await _context.TaskEvents
                .Where(te => te.UserId == member.UserId && te.Task!.ProjectId == projectId)
                .OrderByDescending(te => te.CreatedAt)
                .Select(te => te.CreatedAt)
                .FirstOrDefaultAsync();

            var daysDiff = (dateTo.Value - dateFrom.Value).Days;
            var tasksPerDay = daysDiff > 0 ? (decimal)completedTasks.Count / daysDiff : 0;

            userPerformance.Add(new UserPerformanceDto
            {
                UserId = member.UserId,
                UserName = member.User.UserName ?? member.User.Email ?? "Unknown User",
                UserEmail = member.User.Email ?? "",
                Role = member.Role.ToString(),
                TotalTasksAssigned = assignedTasks.Count,
                TasksCompleted = completedTasks.Count,
                TasksInProgress = inProgressTasks.Count,
                TasksVetoed = vetoedTasks,
                CompletionRate = assignedTasks.Count > 0 ? (decimal)completedTasks.Count / assignedTasks.Count * 100 : 0,
                VetoRate = completedTasks.Count > 0 ? (decimal)vetoedTasks / completedTasks.Count * 100 : 0,
                AverageTaskCompletionTimeHours = averageCompletionTime,
                TasksPerDay = tasksPerDay,
                LastActivityAt = lastActivity,
                DailyProductivity = dailyProductivity
            });
        }

        return userPerformance.OrderByDescending(up => up.TasksCompleted).ToList();
    }

    /// <summary>
    /// Get recent activity for a project
    /// </summary>
    public async Task<List<RecentActivityDto>> GetRecentActivityAsync(int projectId, int limit = 50)
    {
        var recentEvents = await _context.TaskEvents
            .Where(te => te.Task!.ProjectId == projectId)
            .Include(te => te.User)
            .Include(te => te.Task)
            .ThenInclude(t => t!.Asset)
            .Include(te => te.Task)
            .ThenInclude(t => t!.Workflow)
            .Include(te => te.ToWorkflowStage)
            .OrderByDescending(te => te.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var activities = recentEvents.Select(te => new RecentActivityDto
        {
            ActivityType = ConvertEventTypeToActivityType(te.EventType),
            Description = GenerateActivityDescription(te),
            UserId = te.UserId ?? "",
            UserName = te.User != null ? (te.User.UserName ?? te.User.Email ?? "Unknown User") : "System",
            Timestamp = te.CreatedAt,
            AssetName = te.Task?.Asset?.Filename,
            WorkflowName = te.Task?.Workflow?.Name,
            StageName = te.ToWorkflowStage?.Name
        }).ToList();

        return activities;
    }

    /// <summary>
    /// Get project health overview
    /// </summary>
    public async Task<ProjectHealthDto> GetProjectHealthAsync(int projectId)
    {
        var project = await _context.Projects
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.ProjectMembers)
            .Include(p => p.Assets)
            .FirstOrDefaultAsync() ?? throw new ArgumentException("Project not found", nameof(projectId));

        var totalTasks = await _context.Tasks.CountAsync(t => t.ProjectId == projectId);
        var completedTasks = await _context.Tasks.CountAsync(t => t.ProjectId == projectId && t.CompletedAt.HasValue);
        var totalAssets = project.Assets.Count;
        var completedAssets = await _context.Assets
            .CountAsync(a => a.DataSourceId == projectId && 
                _context.Tasks.Any(t => t.AssetId == a.AssetId && t.CompletedAt.HasValue));

        // Calculate quality score (inverse of veto rate)
        var vetoedTasks = await _context.TaskEvents
            .CountAsync(te => te.Task!.ProjectId == projectId && 
                te.EventType == TaskEventType.STATUS_CHANGED &&
                te.Details != null && te.Details.Contains("CHANGES_REQUIRED"));

        var qualityScore = completedTasks > 0 ? Math.Max(0, 100 - (decimal)vetoedTasks / completedTasks * 100) : 100;

        // Calculate productivity score
        var last30Days = DateTime.UtcNow.AddDays(-30);
        var recentlyCompletedTasks = await _context.Tasks
            .CountAsync(t => t.ProjectId == projectId && t.CompletedAt >= last30Days);

        var productivityScore = totalTasks > 0 ? (decimal)recentlyCompletedTasks / totalTasks * 100 : 0;

        // Identify bottlenecks (stages with high task counts and low completion rates)
        var stageBottlenecks = await _context.WorkflowStages
            .Where(ws => ws.Workflow.ProjectId == projectId)
            .Where(ws => ws.TasksAtThisStage.Count > 5 && 
                        ws.TasksAtThisStage.Count(t => !t.CompletedAt.HasValue) > 
                        ws.TasksAtThisStage.Count(t => t.CompletedAt.HasValue))
            .Select(ws => ws.Name)
            .ToListAsync();

        var lastActivity = await _context.TaskEvents
            .Where(te => te.Task!.ProjectId == projectId)
            .OrderByDescending(te => te.CreatedAt)
            .Select(te => te.CreatedAt)
            .FirstOrDefaultAsync();

        return new ProjectHealthDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            OverallCompletionPercentage = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0,
            QualityScore = qualityScore,
            ProductivityScore = Math.Min(100, productivityScore),
            TotalMembers = project.ProjectMembers.Count,
            ActiveMembers = await _context.TaskEvents
                .Where(te => te.Task!.ProjectId == projectId && te.CreatedAt >= last30Days)
                .Select(te => te.UserId)
                .Distinct()
                .CountAsync(),
            TotalAssets = totalAssets,
            CompletedAssets = completedAssets,
            Bottlenecks = stageBottlenecks,
            LastActivityAt = lastActivity
        };
    }

    /// <summary>
    /// Get task statistics for a project
    /// </summary>
    public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(int projectId, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        dateFrom ??= DateTime.UtcNow.AddDays(-30);
        dateTo ??= DateTime.UtcNow;

        var tasksInPeriod = await _context.Tasks
            .Where(t => t.ProjectId == projectId && t.CreatedAt >= dateFrom && t.CreatedAt <= dateTo)
            .ToListAsync();

        var completedTasks = tasksInPeriod.Where(t => t.CompletedAt.HasValue).ToList();
        var vetoedTasks = await _context.TaskEvents
            .CountAsync(te => te.Task!.ProjectId == projectId &&
                             te.CreatedAt >= dateFrom && te.CreatedAt <= dateTo &&
                             te.EventType == TaskEventType.STATUS_CHANGED &&
                             te.Details != null && te.Details.Contains("CHANGES_REQUIRED"));

        var averageCompletionTime = CalculateAverageCompletionTime(completedTasks);

        // Calculate productivity trend (compare with previous period)
        var previousPeriodFrom = dateFrom.Value.AddDays(-(dateTo.Value - dateFrom.Value).Days);
        var previousPeriodTasks = await _context.Tasks
            .CountAsync(t => t.ProjectId == projectId && 
                           t.CreatedAt >= previousPeriodFrom && t.CreatedAt < dateFrom &&
                           t.CompletedAt.HasValue);

        var productivityTrend = previousPeriodTasks > 0 ? 
            ((decimal)completedTasks.Count - previousPeriodTasks) / previousPeriodTasks * 100 : 0;

        // Status distribution
        var statusDistribution = new List<TaskStatusDistributionDto>
        {
            new() {
                Status = "Completed",
                Count = completedTasks.Count, Percentage = tasksInPeriod.Count > 0 ?
                (decimal)completedTasks.Count / tasksInPeriod.Count * 100 : 0
            },
            new() {
                Status = "In Progress",
                Count = tasksInPeriod.Count(t => !t.CompletedAt.HasValue && t.AssignedToUserId != null),
                Percentage = 0
            },
            new() {
                Status = "Not Started",
                Count = tasksInPeriod.Count(t => !t.CompletedAt.HasValue && t.AssignedToUserId == null),
                Percentage = 0
            },
            new() {
                Status = "Suspended",
                Count = tasksInPeriod.Count(t => t.SuspendedAt.HasValue),
                Percentage = 0
            },
            new() {
                Status = "Deferred",
                Count = tasksInPeriod.Count(t => t.DeferredAt.HasValue),
                Percentage = 0
            }
        };

        // Calculate percentages
        foreach (var status in statusDistribution.Skip(1))
        {
            status.Percentage = tasksInPeriod.Count > 0 ? (decimal)status.Count / tasksInPeriod.Count * 100 : 0;
        }

        // Daily task counts
        var dailyTaskCounts = new List<DailyTaskCountDto>();
        for (var date = dateFrom.Value.Date; date <= dateTo.Value.Date; date = date.AddDays(1))
        {
            var dayTasks = tasksInPeriod.Where(t => t.CreatedAt.Date == date).ToList();
            var dayCompleted = dayTasks.Count(t => t.CompletedAt?.Date == date);
            var dayVetoed = await _context.TaskEvents
                .CountAsync(te => te.Task!.ProjectId == projectId &&
                    te.CreatedAt.Date == date &&
                    te.EventType == TaskEventType.STATUS_CHANGED &&
                    te.Details != null && te.Details.Contains("CHANGES_REQUIRED"));

            dailyTaskCounts.Add(new DailyTaskCountDto
            {
                Date = date,
                TasksCreated = dayTasks.Count,
                TasksCompleted = dayCompleted,
                TasksVetoed = dayVetoed
            });
        }

        return new TaskStatisticsDto
        {
            DateRange = dateTo.Value - dateFrom.Value,
            TotalTasks = tasksInPeriod.Count,
            CompletedTasks = completedTasks.Count,
            VetoedTasks = vetoedTasks,
            AverageCompletionTimeHours = averageCompletionTime,
            ProductivityTrend = productivityTrend,
            StatusDistribution = statusDistribution,
            DailyTaskCounts = dailyTaskCounts
        };
    }

    /// <summary>
    /// Get workflow stage progress analytics
    /// </summary>
    public async Task<List<WorkflowStageProgressDto>> GetWorkflowStageProgressAsync(int projectId, int workflowId)
    {
        var stages = await _context.WorkflowStages
            .Where(ws => ws.WorkflowId == workflowId)
            .Include(ws => ws.TasksAtThisStage)
            .OrderBy(ws => ws.StageOrder)
            .ToListAsync();

        var stageProgress = new List<WorkflowStageProgressDto>();

        foreach (var stage in stages)
        {
            var tasks = stage.TasksAtThisStage.ToList();
            var completedTasks = tasks.Where(t => t.CompletedAt.HasValue).ToList();
            var inProgressTasks = tasks.Count(t => !t.CompletedAt.HasValue && t.AssignedToUserId != null);

            var averageCompletionTime = CalculateAverageCompletionTime(completedTasks);

            var lastActivity = await _context.TaskEvents
                .Where(te => te.ToWorkflowStageId == stage.WorkflowStageId || 
                            te.Task!.CurrentWorkflowStageId == stage.WorkflowStageId)
                .OrderByDescending(te => te.CreatedAt)
                .Select(te => te.CreatedAt)
                .FirstOrDefaultAsync();

            stageProgress.Add(new WorkflowStageProgressDto
            {
                WorkflowStageId = stage.WorkflowStageId,
                StageName = stage.Name,
                StageOrder = stage.StageOrder,
                StageType = stage.StageType?.ToString() ?? "",
                TotalTasks = tasks.Count,
                CompletedTasks = completedTasks.Count,
                InProgressTasks = inProgressTasks,
                AverageCompletionTimeHours = averageCompletionTime,
                LastActivityAt = lastActivity
            });
        }

        return stageProgress;
    }

    #region Private Helper Methods

    private static decimal CalculateAverageCompletionTime(List<Task> completedTasks)
    {
        if (!completedTasks.Any()) return 0;

        var totalHours = 0.0;
        var validTasks = 0;

        foreach (var task in completedTasks.Where(t => t.CompletedAt.HasValue))
        {
            var completionTime = (task.CompletedAt!.Value - task.CreatedAt).TotalHours;
            if (completionTime > 0)
            {
                totalHours += completionTime;
                validTasks++;
            }
        }

        return validTasks > 0 ? (decimal)(totalHours / validTasks) : 0;
    }

    private async Task<List<DailyProductivityDto>> CalculateDailyProductivity(string userId, int projectId, DateTime dateFrom, DateTime dateTo)
    {
        var dailyProductivity = new List<DailyProductivityDto>();

        for (var date = dateFrom.Date; date <= dateTo.Date; date = date.AddDays(1))
        {
            var dayCompleted = await _context.Tasks
                .CountAsync(t => t.ProjectId == projectId && 
                               t.LastWorkedOnByUserId == userId &&
                               t.CompletedAt.HasValue && 
                               t.CompletedAt.Value.Date == date);

            // Estimate hours worked based on task events (simplified calculation)
            var dayEvents = await _context.TaskEvents
                .CountAsync(te => te.UserId == userId && 
                                 te.Task!.ProjectId == projectId &&
                                 te.CreatedAt.Date == date);

            var estimatedHours = Math.Min(8, dayEvents * 0.5m); // Rough estimate

            dailyProductivity.Add(new DailyProductivityDto
            {
                Date = date,
                TasksCompleted = dayCompleted,
                HoursWorked = estimatedHours
            });
        }

        return dailyProductivity;
    }

    private static string ConvertEventTypeToActivityType(TaskEventType eventType)
    {
        return eventType switch
        {
            TaskEventType.ANNOTATION_CREATED => "annotation",
            TaskEventType.ANNOTATION_UPDATED => "annotation",
            TaskEventType.REVIEW_SUBMITTED => "review",
            TaskEventType.TASK_ASSIGNED => "assignment",
            TaskEventType.TASK_CREATED => "upload",
            TaskEventType.STATUS_CHANGED => "completion",
            _ => "other"
        };
    }

    private static string GenerateActivityDescription(server.Models.Domain.TaskEvent taskEvent)
    {
        return taskEvent.EventType switch
        {
            TaskEventType.TASK_CREATED => "Created a new task",
            TaskEventType.TASK_ASSIGNED => "Assigned task to user",
            TaskEventType.STATUS_CHANGED => "Changed task status",
            TaskEventType.ANNOTATION_CREATED => "Created annotation",
            TaskEventType.ANNOTATION_UPDATED => "Updated annotation",
            TaskEventType.REVIEW_SUBMITTED => "Submitted review",
            TaskEventType.STAGE_CHANGED => $"Moved task to {taskEvent.ToWorkflowStage?.Name}",
            _ => $"{taskEvent.EventType.ToString().Replace('_', ' ').ToLowerInvariant()}"
        };
    }

    #endregion
}