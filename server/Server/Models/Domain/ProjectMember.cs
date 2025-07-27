using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class ProjectMember
{
    public int ProjectMemberId { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.VIEWER;
    public DateTime InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    public int ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<WorkflowStageAssignment> WorkflowStageAssignments { get; set; } = [];
}
