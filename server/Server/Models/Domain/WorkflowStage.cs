using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class WorkflowStage
{
    public int WorkflowStageId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int StageOrder { get; init; }
    public WorkflowStageType? StageType { get; init; }
    public bool IsInitialStage { get; init; }
    public bool IsFinalStage { get; init; }
    public string? UiConfiguration { get; init; } // TODO: For JSONB, store as string for now

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Keys
    public int WorkflowId { get; init; }
    public int? InputDataSourceId { get; init; }
    public int? TargetDataSourceId { get; init; }

    // Navigation Properties
    public virtual Workflow Workflow { get; init; } = null!; // Required parent workflow
    public virtual DataSource? InputDataSource { get; init; }
    public virtual DataSource? TargetDataSource { get; init; }

    // Tasks currently at this stage
    public virtual ICollection<Task> TasksAtThisStage { get; init; } = [];

    // Many-to-many relationship with ApplicationUser
    public virtual ICollection<ApplicationUser> AssignedUsers { get; init; } = [];
}
