using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Asset;

public record class CreateAssetDto
{
    [Required]
    [StringLength(2048, MinimumLength = 1)]
    public string ExternalId { get; init; } = string.Empty;

    [StringLength(255)]
    public string? Filename { get; init; }

    public string? MimeType { get; init; }

    public long? SizeBytes { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? DurationMs { get; init; }

    public string? Metadata { get; init; }

    public int? DataSourceId { get; init; }
}
