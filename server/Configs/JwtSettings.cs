using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public class JwtSettings
{
    public const string SectionName = "JWT";

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:Secret is required and cannot be empty.")]
    public string Secret { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:ValidIssuer is required and cannot be empty.")]
    public string ValidIssuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:ValidAudience is required and cannot be empty.")]
    public string ValidAudience { get; set; } = string.Empty;

    public string? Authority { get; set; }
}
