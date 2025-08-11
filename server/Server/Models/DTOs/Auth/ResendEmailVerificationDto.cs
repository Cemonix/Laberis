using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Auth;

public class ResendEmailVerificationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}