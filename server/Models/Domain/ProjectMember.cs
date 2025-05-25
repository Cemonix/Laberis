using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class ProjectMember
{
    public int ProjectMemberId { get; init; }
    public ProjectRole Role { get; init; }
    public DateTime InvitedAt { get; init; }
    public DateTime? JoinedAt { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Keys
    public int ProjectId { get; init; }
    public string UserId { get; init; } = string.Empty;

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;
    public virtual ApplicationUser User { get; init; } = null!;
}
