namespace server.Models.DTOs.Asset;

/// <summary>
/// DTO for representing the result of a bulk upload operation
/// </summary>
public record class BulkUploadResultDto
{
    /// <summary>
    /// List of individual upload results
    /// </summary>
    public List<UploadResultDto> Results { get; init; } = new();

    /// <summary>
    /// Total number of files processed
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Number of successful uploads
    /// </summary>
    public int SuccessfulUploads { get; init; }

    /// <summary>
    /// Number of failed uploads
    /// </summary>
    public int FailedUploads { get; init; }

    /// <summary>
    /// Overall success status
    /// </summary>
    public bool AllSuccessful => FailedUploads == 0;

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary { get; init; } = string.Empty;
}
