using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.TaskEvent;

public record class CreateTaskEventDto
{
    [Required]
    public TaskEventType EventType { get; init; }

    public string? Details { get; init; }

    [Required]
    public int TaskId { get; init; }

    [Required]
    public string? UserId { get; init; }

    public int? FromWorkflowStageId { get; init; }

    public int? ToWorkflowStageId { get; init; }
}
