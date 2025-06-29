using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP host is required.")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP password is required.")]
    public string Password { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "From name is required.")]
    public string FromName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "From address is required.")]
    public string FromAddress { get; set; } = string.Empty;
}