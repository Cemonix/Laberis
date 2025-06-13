using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Asset;

public record class UpdateAssetDto
{
    [StringLength(255)]
    public string? Filename { get; init; }

    public string? MimeType { get; init; }

    public long? SizeBytes { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? DurationMs { get; init; }

    public string? Metadata { get; init; }

    [Required]
    public AssetStatus Status { get; init; }

    public int? DataSourceId { get; init; }
}
