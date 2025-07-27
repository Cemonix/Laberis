using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class ProjectInvitation
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public ProjectRole Role { get; set; } = ProjectRole.VIEWER;
    public string InvitationToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; } = false;
    public string? InvitedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ApplicationUser? InvitedByUser { get; set; }
}
