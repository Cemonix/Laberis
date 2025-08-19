namespace server.Models.DTOs.Task;

public class ChangeTaskStatusDto
{
    /// <summary>
    /// The target status to change the task to.
    /// </summary>
    public Domain.Enums.TaskStatus TargetStatus { get; set; }
}