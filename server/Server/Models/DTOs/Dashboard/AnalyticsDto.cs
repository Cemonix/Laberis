namespace server.Models.DTOs.Dashboard;

/// <summary>
/// DTO for workflow progress analytics
/// </summary>
public class WorkflowProgressDto
{
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public int SuspendedTasks { get; set; }
    public int DeferredTasks { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<WorkflowStageProgressDto> StageProgress { get; set; } = [];
}

/// <summary>
/// DTO for workflow stage progress analytics
/// </summary>
public class WorkflowStageProgressDto
{
    public int WorkflowStageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string StageType { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public decimal AverageCompletionTimeHours { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>
/// DTO for user performance analytics
/// </summary>
public class UserPerformanceDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TotalTasksAssigned { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksInProgress { get; set; }
    public int TasksVetoed { get; set; } // Tasks returned for rework
    public decimal CompletionRate { get; set; }
    public decimal VetoRate { get; set; }
    public decimal AverageTaskCompletionTimeHours { get; set; }
    public decimal TasksPerDay { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public List<DailyProductivityDto> DailyProductivity { get; set; } = [];
}

/// <summary>
/// DTO for daily productivity metrics
/// </summary>
public class DailyProductivityDto
{
    public DateTime Date { get; set; }
    public int TasksCompleted { get; set; }
    public decimal HoursWorked { get; set; }
}

/// <summary>
/// DTO for recent activity analytics
/// </summary>
public class RecentActivityDto
{
    public string ActivityType { get; set; } = string.Empty; // "task_completed", "annotation_created", etc.
    public string Description { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AssetName { get; set; }
    public string? WorkflowName { get; set; }
    public string? StageName { get; set; }
}

/// <summary>
/// DTO for project health overview
/// </summary>
public class ProjectHealthDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal OverallCompletionPercentage { get; set; }
    public decimal QualityScore { get; set; } // Based on veto rates
    public decimal ProductivityScore { get; set; } // Based on task completion rates
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int TotalAssets { get; set; }
    public int CompletedAssets { get; set; }
    public List<string> Bottlenecks { get; set; } = []; // Stage names with delays
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// DTO for task statistics analytics
/// </summary>
public class TaskStatisticsDto
{
    public TimeSpan DateRange { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int VetoedTasks { get; set; }
    public decimal AverageCompletionTimeHours { get; set; }
    public decimal ProductivityTrend { get; set; } // Percentage change vs previous period
    public List<TaskStatusDistributionDto> StatusDistribution { get; set; } = [];
    public List<DailyTaskCountDto> DailyTaskCounts { get; set; } = [];
}

/// <summary>
/// DTO for task status distribution
/// </summary>
public class TaskStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for daily task counts
/// </summary>
public class DailyTaskCountDto
{
    public DateTime Date { get; set; }
    public int TasksCreated { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksVetoed { get; set; }
}

/// <summary>
/// DTO for comprehensive dashboard analytics
/// </summary>
public class DashboardAnalyticsDto
{
    public ProjectHealthDto ProjectHealth { get; set; } = new();
    public List<WorkflowProgressDto> WorkflowProgress { get; set; } = [];
    public List<UserPerformanceDto> UserPerformance { get; set; } = [];
    public List<RecentActivityDto> RecentActivities { get; set; } = [];
    public TaskStatisticsDto TaskStatistics { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}