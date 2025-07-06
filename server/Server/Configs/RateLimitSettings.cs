using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    [Required]
    [Range(1, 10000)]
    public int GlobalPermitLimit { get; set; } = 100;

    [Required]
    [Range(1, 3600)]
    public int GlobalWindowInSeconds { get; set; } = 60;

    [Required]
    [Range(0, 100)]
    public int GlobalQueueLimit { get; set; } = 10;

    // Authentication endpoints (more restrictive)
    [Required]
    [Range(1, 100)]
    public int AuthPermitLimit { get; set; } = 5;

    [Required]
    [Range(1, 3600)]
    public int AuthWindowInSeconds { get; set; } = 60;

    [Required]
    [Range(0, 20)]
    public int AuthQueueLimit { get; set; } = 0;

    // File upload endpoints (more restrictive)
    [Required]
    [Range(1, 100)]
    public int UploadPermitLimit { get; set; } = 10;

    [Required]
    [Range(1, 3600)]
    public int UploadWindowInSeconds { get; set; } = 60;

    [Required]
    [Range(0, 20)]
    public int UploadQueueLimit { get; set; } = 2;

    // Project management endpoints (moderate)
    [Required]
    [Range(1, 1000)]
    public int ProjectPermitLimit { get; set; } = 50;

    [Required]
    [Range(1, 3600)]
    public int ProjectWindowInSeconds { get; set; } = 60;

    [Required]
    [Range(0, 50)]
    public int ProjectQueueLimit { get; set; } = 5;

    // Public/read-only endpoints (less restrictive)
    [Required]
    [Range(1, 10000)]
    public int PublicPermitLimit { get; set; } = 200;

    [Required]
    [Range(1, 3600)]
    public int PublicWindowInSeconds { get; set; } = 60;

    [Required]
    [Range(0, 100)]
    public int PublicQueueLimit { get; set; } = 20;
}
