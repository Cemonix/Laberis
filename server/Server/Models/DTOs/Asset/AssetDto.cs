using server.Models.Domain.Enums;

namespace server.Models.DTOs.Asset;

public record class AssetDto
{
    public int Id { get; init; }
    public string Filename { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public long? SizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationMs { get; init; }
    public string? Metadata { get; init; }
    public AssetStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ProjectId { get; init; }
    public int DataSourceId { get; init; }
    
    /// <summary>
    /// Presigned URL for accessing the asset (temporary, expires)
    /// </summary>
    public string? ImageUrl { get; init; }
}
