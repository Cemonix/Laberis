namespace server.Models.DTOs.Task;

/// <summary>
/// Response DTO for task navigation operations
/// </summary>
public class TaskNavigationDto
{
    /// <summary>
    /// ID of the next/previous task, null if none available
    /// </summary>
    public int? TaskId { get; set; }

    /// <summary>
    /// Asset ID of the next/previous task, null if none available
    /// </summary>
    public int? AssetId { get; set; }

    /// <summary>
    /// Whether there is a next task available
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Whether there is a previous task available
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Current position in the navigation sequence (1-based)
    /// </summary>
    public int CurrentPosition { get; set; }

    /// <summary>
    /// Total number of navigable tasks
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Optional message for user feedback
    /// </summary>
    public string? Message { get; set; }
}