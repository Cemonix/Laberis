using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class TaskEvent
{
    public long EventId { get; set; }
    public TaskEventType EventType { get; set; }
    public string? Details { get; set; } // TODO: For JSONB, store as string
    public DateTime CreatedAt { get; set; } // No UpdatedAt for immutable events

    // Foreign Keys
    public int TaskId { get; set; }
    public string? UserId { get; set; }
    public int? FromWorkflowStageId { get; set; }
    public int? ToWorkflowStageId { get; set; }

    // Navigation Properties
    public virtual Task? Task { get; set; }
    public virtual ApplicationUser? User { get; set; }
    public virtual WorkflowStage? FromWorkflowStage { get; set; }
    public virtual WorkflowStage? ToWorkflowStage { get; set; }
}
