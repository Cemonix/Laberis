using server.Models.Domain.Enums;

namespace server.Models.DTOs.ProjectMember;

public record class ProjectMemberDto
{
    public int Id { get; init; }
    public ProjectRole Role { get; init; }
    public DateTime InvitedAt { get; init; }
    public DateTime? JoinedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ProjectId { get; init; }
    public string UserId { get; init; } = string.Empty;
}
