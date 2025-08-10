namespace server.Models.Domain;

public class Workflow
{
    public int WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Key to Project
    public int ProjectId { get; set; }

    // Foreign Key to LabelScheme
    public int LabelSchemeId { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual LabelScheme LabelScheme { get; set; } = null!;

    // A workflow consists of multiple stages
    public virtual ICollection<WorkflowStage> WorkflowStages { get; set; } = [];

    // A workflow definition is used by many tasks
    public virtual ICollection<Task> Tasks { get; set; } = [];
}
