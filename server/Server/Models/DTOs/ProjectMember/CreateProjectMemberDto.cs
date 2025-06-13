using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectMember;

public record class CreateProjectMemberDto
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public ProjectRole Role { get; init; } = ProjectRole.VIEWER;
}
