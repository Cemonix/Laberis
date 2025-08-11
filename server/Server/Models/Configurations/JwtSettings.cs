using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public record JwtSettings
{
    public const string SectionName = "JWT";

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:Secret is required and cannot be empty.")]
    public string Secret { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "JWT:Expiration must be a positive integer.")]
    public int Expiration { get; init; } = 60; // Default to 60 minutes

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:ValidIssuer is required and cannot be empty.")]
    public string ValidIssuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT:ValidAudience is required and cannot be empty.")]
    public string ValidAudience { get; init; } = string.Empty;

    [Range(1, 365, ErrorMessage = "JWT:RefreshTokenExpiration must be between 1 and 365 days.")]
    public int RefreshTokenExpiration { get; init; } = 7; // Default to 7 days

    public string? Authority { get; init; }
}
