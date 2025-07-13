using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public record SmtpSettings
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP host is required.")]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP username is required.")]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "SMTP password is required.")]
    public string Password { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "From name is required.")]
    public string FromName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "From address is required.")]
    public string FromAddress { get; init; } = string.Empty;
}