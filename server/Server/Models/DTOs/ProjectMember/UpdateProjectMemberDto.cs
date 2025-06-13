using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectMember;

public record class UpdateProjectMemberDto
{
    [Required]
    public ProjectRole Role { get; init; }
}
