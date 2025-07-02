namespace server.Models.Domain;

public record class Workflow
{
    public int WorkflowId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Key to Project
    public int ProjectId { get; init; }

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;

    // A workflow consists of multiple stages
    public virtual ICollection<WorkflowStage> WorkflowStages { get; init; } = [];

    // A workflow definition is used by many tasks
    public virtual ICollection<Task> Tasks { get; init; } = [];
}
