using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class ProjectInvitation
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Email { get; init; } = string.Empty;
    public ProjectRole Role { get; init; } = ProjectRole.VIEWER;
    public string InvitationToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsAccepted { get; init; } = false;
    public string? InvitedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;
    public virtual ApplicationUser? InvitedByUser { get; init; }
}
