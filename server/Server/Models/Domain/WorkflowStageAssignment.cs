using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

/// <summary>
/// Represents the assignment of a user to a specific workflow stage with a defined role.
/// This allows tracking who can work on which stages and in what capacity.
/// </summary>
public record class WorkflowStageAssignment
{
    public int WorkflowStageAssignmentId { get; init; }
    
    // Foreign Keys
    public int WorkflowStageId { get; init; }
    public int ProjectMemberId { get; init; }
    
    // Navigation Properties
    public virtual WorkflowStage WorkflowStage { get; init; } = null!;
    public virtual ProjectMember ProjectMember { get; init; } = null!;
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
