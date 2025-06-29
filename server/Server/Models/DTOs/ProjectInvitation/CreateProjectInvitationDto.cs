using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectInvitation;

public record class CreateProjectInvitationDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    [Required]
    public ProjectRole Role { get; init; } = ProjectRole.VIEWER;
}
