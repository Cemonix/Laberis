using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Asset;

/// <summary>
/// DTO for uploading a single asset file
/// </summary>
public record class UploadAssetDto
{
    /// <summary>
    /// The file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; init; } = default!;

    /// <summary>
    /// The data source ID to associate with this asset
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DataSourceId must be a positive integer")]
    public int DataSourceId { get; init; }

    /// <summary>
    /// Optional metadata for the asset as JSON string
    /// </summary>
    public string? Metadata { get; init; }
}
