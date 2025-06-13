using server.Models.Domain.Enums;

namespace server.Models.DTOs.TaskEvent;

public record class TaskEventDto
{
    public long Id { get; init; }
    public TaskEventType EventType { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
    public int TaskId { get; init; }
    public string? UserId { get; init; }
    public int? FromWorkflowStageId { get; init; }
    public int? ToWorkflowStageId { get; init; }
}
