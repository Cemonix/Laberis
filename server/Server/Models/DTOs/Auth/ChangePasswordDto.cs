using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Auth;

public class ChangePasswordDto
{
    [Required]
    [StringLength(100, ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}