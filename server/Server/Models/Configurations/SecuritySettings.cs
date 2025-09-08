using System.ComponentModel.DataAnnotations;

namespace server.Configs;

/// <summary>
/// Security-related application settings.
/// </summary>
public record SecuritySettings
{
    public const string SectionName = "Security";

    /// <summary>
    /// Default expiry for presigned URLs in seconds.
    /// Keep small in production to reduce exposure window.
    /// </summary>
    [Range(60, 86400)]
    public int PresignedUrlExpirySeconds { get; init; } = 300; // 5 minutes
}

