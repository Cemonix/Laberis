using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Task;

public record class CreateTaskDto
{
    [Required]
    public int AssetId { get; init; }

    [Required]
    public int WorkflowId { get; init; }

    [Required]
    public int WorkflowStageId { get; init; }

    public int Priority { get; init; } = 0;

    public Domain.Enums.TaskStatus Status { get; init; } = Domain.Enums.TaskStatus.NOT_STARTED;

    public DateTime? DueDate { get; init; }

    public string? Metadata { get; init; }

    public string? AssignedToUserId { get; init; }
}
