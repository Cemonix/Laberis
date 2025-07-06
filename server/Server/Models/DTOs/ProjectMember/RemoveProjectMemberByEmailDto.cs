using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectMember;

public record class RemoveProjectMemberByEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
