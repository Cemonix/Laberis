using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public record RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    [Required]
    [Range(1, 10000)]
    public int GlobalPermitLimit { get; init; } = 100;

    [Required]
    [Range(1, 3600)]
    public int GlobalWindowInSeconds { get; init; } = 60;

    [Required]
    [Range(0, 100)]
    public int GlobalQueueLimit { get; init; } = 10;

    // Authentication endpoints (more restrictive)
    [Required]
    [Range(1, 100)]
    public int AuthPermitLimit { get; init; } = 5;

    [Required]
    [Range(1, 3600)]
    public int AuthWindowInSeconds { get; init; } = 60;

    [Required]
    [Range(0, 20)]
    public int AuthQueueLimit { get; init; } = 0;

    // File upload endpoints (more restrictive)
    [Required]
    [Range(1, 100)]
    public int UploadPermitLimit { get; init; } = 10;

    [Required]
    [Range(1, 3600)]
    public int UploadWindowInSeconds { get; init; } = 60;

    [Required]
    [Range(0, 20)]
    public int UploadQueueLimit { get; init; } = 2;

    // Project management endpoints (moderate)
    [Required]
    [Range(1, 1000)]
    public int ProjectPermitLimit { get; init; } = 50;

    [Required]
    [Range(1, 3600)]
    public int ProjectWindowInSeconds { get; init; } = 60;

    [Required]
    [Range(0, 50)]
    public int ProjectQueueLimit { get; init; } = 5;

    // Public/read-only endpoints (less restrictive)
    [Required]
    [Range(1, 10000)]
    public int PublicPermitLimit { get; init; } = 200;

    [Required]
    [Range(1, 3600)]
    public int PublicWindowInSeconds { get; init; } = 60;

    [Required]
    [Range(0, 100)]
    public int PublicQueueLimit { get; init; } = 20;
}
