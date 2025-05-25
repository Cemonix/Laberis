namespace server.Models.Domain;

public record class Task
{
    public int TaskId { get; init; }
    public int Priority { get; init; }
    public string? Status { get; init; } // TODO: Decide if needed
    public DateTime? DueDate { get; init; }
    public string? Metadata { get; init; } // TODO: For JSONB, store as string for now
    public DateTime? CompletedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Keys
    public int AssetId { get; init; }
    public int ProjectId { get; init; }
    public int WorkflowId { get; init; }
    public int CurrentWorkflowStageId { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? LastWorkedOnByUserId { get; init; }

    // Navigation Properties
    public virtual Asset Asset { get; init; } = null!;
    public virtual Project Project { get; init; } = null!;
    public virtual Workflow Workflow { get; init; } = null!;
    public virtual WorkflowStage CurrentWorkflowStage { get; init; } = null!;
    public virtual ApplicationUser? AssignedToUser { get; init; }
    public virtual ApplicationUser? LastWorkedOnByUser { get; init; }

    public virtual ICollection<Annotation> Annotations { get; init; } = [];
    public virtual ICollection<TaskEvent> TaskEvents { get; init; } = [];
    public virtual ICollection<Issue> Issues { get; init; } = [];
}
