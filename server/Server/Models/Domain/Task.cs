using Microsoft.AspNetCore.Identity;

namespace server.Models.Domain;

public class Task
{
    public int TaskId { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Metadata { get; set; } // TODO: For JSONB, store as string for now
    public DateTime? CompletedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    public int AssetId { get; set; }
    public int ProjectId { get; set; }
    public int WorkflowId { get; set; }
    public int CurrentWorkflowStageId { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? LastWorkedOnByUserId { get; set; }

    // Navigation Properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual Project Project { get; set; } = null!;
    public virtual Workflow Workflow { get; set; } = null!;
    public virtual WorkflowStage CurrentWorkflowStage { get; set; } = null!;
    public virtual ApplicationUser? AssignedToUser { get; set; }
    public virtual ApplicationUser? LastWorkedOnByUser { get; set; }

    public virtual ICollection<Annotation> Annotations { get; set; } = [];
    public virtual ICollection<TaskEvent> TaskEvents { get; set; } = [];
    public virtual ICollection<Issue> Issues { get; set; } = [];
}
