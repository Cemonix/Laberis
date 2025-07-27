using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

/// <summary>
/// Represents the assignment of a user to a specific workflow stage with a defined role.
/// This allows tracking who can work on which stages and in what capacity.
/// </summary>
public class WorkflowStageAssignment
{
    public int WorkflowStageAssignmentId { get; set; }
    
    // Foreign Keys
    public int WorkflowStageId { get; set; }
    public int ProjectMemberId { get; set; }
    
    // Navigation Properties
    public virtual WorkflowStage WorkflowStage { get; set; } = null!;
    public virtual ProjectMember ProjectMember { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
