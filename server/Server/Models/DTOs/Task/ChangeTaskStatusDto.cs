using server.Models.Domain.Enums;

namespace server.Models.DTOs.Task;

public class ChangeTaskStatusDto
{
    /// <summary>
    /// The target status to change the task to.
    /// </summary>
    public server.Models.Domain.Enums.TaskStatus TargetStatus { get; set; }
    
    /// <summary>
    /// Whether to move the asset to the appropriate data source based on workflow progression.
    /// Defaults to true. Set to false for status changes that shouldn't trigger asset movement.
    /// </summary>
    public bool MoveAsset { get; set; } = true;
}