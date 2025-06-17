namespace server.Exceptions;

/// <summary>
/// Enumeration of standard error types used across the application
/// </summary>
public enum ErrorTypes
{
    ValidationError,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    InternalServerError,
    BadRequest,
    ServiceUnavailable,
    FileTooLarge,
    UnsupportedFileType,
    StorageError,
    DatabaseError
}

/// <summary>
/// Extension methods for ErrorTypes enum
/// </summary>
public static class ErrorTypesExtensions
{
    /// <summary>
    /// Converts ErrorTypes enum to string representation
    /// </summary>
    public static string ToStringValue(this ErrorTypes errorType)
    {
        return errorType switch
        {
            ErrorTypes.ValidationError => "validation_error",
            ErrorTypes.NotFound => "not_found",
            ErrorTypes.Unauthorized => "unauthorized",
            ErrorTypes.Forbidden => "forbidden",
            ErrorTypes.Conflict => "conflict",
            ErrorTypes.InternalServerError => "internal_server_error",
            ErrorTypes.BadRequest => "bad_request",
            ErrorTypes.ServiceUnavailable => "service_unavailable",
            ErrorTypes.FileTooLarge => "file_too_large",
            ErrorTypes.UnsupportedFileType => "unsupported_file_type",
            ErrorTypes.StorageError => "storage_error",
            ErrorTypes.DatabaseError => "database_error",
            _ => "unknown_error"
        };
    }
}
