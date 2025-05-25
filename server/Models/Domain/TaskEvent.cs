using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class TaskEvent
{
    public long EventId { get; init; }
    public TaskEventType EventType { get; init; }
    public string? Details { get; init; } // TODO: For JSONB, store as string
    public DateTime CreatedAt { get; init; } // No UpdatedAt for immutable events

    // Foreign Keys
    public int TaskId { get; init; }
    public string? UserId { get; init; }
    public int? FromWorkflowStageId { get; init; }
    public int? ToWorkflowStageId { get; init; }

    // Navigation Properties
    public virtual Task Task { get; init; } = null!;
    public virtual ApplicationUser? User { get; init; }
    public virtual WorkflowStage? FromWorkflowStage { get; init; }
    public virtual WorkflowStage? ToWorkflowStage { get; init; }
}
