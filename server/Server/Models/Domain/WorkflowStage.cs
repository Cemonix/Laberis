using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class WorkflowStage
{
    public int WorkflowStageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StageOrder { get; set; }
    public WorkflowStageType StageType { get; set; }
    public bool IsInitialStage { get; set; }
    public bool IsFinalStage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    public int WorkflowId { get; set; }
    public int? InputDataSourceId { get; set; }
    public int? TargetDataSourceId { get; set; }

    // Navigation Properties
    public virtual Workflow Workflow { get; set; } = null!; // Required parent workflow
    public virtual DataSource? InputDataSource { get; set; }
    public virtual DataSource? TargetDataSource { get; set; }

    // Stage Pipeline Relationships via connections
    public virtual ICollection<WorkflowStageConnection> IncomingConnections { get; set; } = [];
    public virtual ICollection<WorkflowStageConnection> OutgoingConnections { get; set; } = [];

    // Tasks currently at this stage
    public virtual ICollection<Task> TasksAtThisStage { get; set; } = [];

    // User assignments with roles for this stage
    public virtual ICollection<WorkflowStageAssignment> StageAssignments { get; set; } = [];
}
