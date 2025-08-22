namespace server.Models.DTOs.Task;

/// <summary>
/// DTO for task completion requests
/// </summary>
public class CompleteTaskDto
{
    /// <summary>
    /// Optional notes or comments about the task completion
    /// </summary>
    public string? Notes { get; set; }
}