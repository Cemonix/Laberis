using server.Models.Domain.Enums;

namespace server.Models.DTOs.ProjectInvitation;

public record class ProjectInvitationDto
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Email { get; init; } = string.Empty;
    public ProjectRole Role { get; init; }
    public string InvitationToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsAccepted { get; init; }
    public string? InvitedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
