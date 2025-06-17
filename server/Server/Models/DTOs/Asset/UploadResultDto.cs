namespace server.Models.DTOs.Asset;

/// <summary>
/// DTO for representing the result of an upload operation
/// </summary>
public record class UploadResultDto
{
    /// <summary>
    /// The uploaded asset if successful
    /// </summary>
    public AssetDto? Asset { get; init; }

    /// <summary>
    /// The original filename
    /// </summary>
    public string Filename { get; init; } = string.Empty;

    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error type if upload failed
    /// </summary>
    public string? ErrorType { get; init; }
}
