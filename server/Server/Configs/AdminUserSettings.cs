using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public class AdminUserSettings
{
    public const string SectionName = "DefaultAdminUser";

    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin user email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format for admin user.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin password is required.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin role is required.")]
    public string Role { get; set; } = string.Empty;
}
