namespace server.Models.DTOs.Task;

public class ReturnTaskForReworkDto
{
    /// <summary>
    /// Optional reason for returning the task for rework.
    /// </summary>
    public string? Reason { get; set; }
}