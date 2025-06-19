namespace server.Models.Common;

using server.Exceptions;

/// <summary>
/// Standardized error response for API endpoints
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Error type/category
    /// </summary>
    public ErrorType Type { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Detailed error description (optional, for debugging)
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Validation errors (if applicable)
    /// </summary>
    public Dictionary<string, List<string>>? ValidationErrors { get; init; }

    /// <summary>
    /// Unique trace identifier for logging
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional error metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
