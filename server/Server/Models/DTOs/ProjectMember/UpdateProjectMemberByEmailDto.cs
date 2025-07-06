using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectMember;

public record class UpdateProjectMemberByEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    public ProjectRole Role { get; init; }
}
