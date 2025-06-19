using server.Models.Domain.Enums;

namespace server.Models.DTOs.Asset;

/// <summary>
/// A reduced asset DTO for frontend consumption that excludes sensitive internal details
/// </summary>
public record class AssetSummaryDto
{
    public int Id { get; init; }
    public string? Filename { get; init; }
    public string? MimeType { get; init; }
    public long? SizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationMs { get; init; }
    public AssetStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    /// <summary>
    /// Presigned URL for accessing the asset (temporary, expires)
    /// </summary>
    public string? ImageUrl { get; init; }
}
