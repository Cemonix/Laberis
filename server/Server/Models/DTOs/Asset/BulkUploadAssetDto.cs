using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Asset;

/// <summary>
/// DTO for bulk uploading multiple asset files
/// </summary>
public record class BulkUploadAssetDto
{
    /// <summary>
    /// The files to upload
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one file must be provided")]
    public IFormFileCollection Files { get; init; } = default!;

    /// <summary>
    /// The data source ID to associate with these assets
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DataSourceId must be a positive integer")]
    public int DataSourceId { get; init; }

    /// <summary>
    /// Optional metadata for all assets as JSON string
    /// </summary>
    public string? Metadata { get; init; }
}
