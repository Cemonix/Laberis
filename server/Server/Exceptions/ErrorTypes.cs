namespace server.Exceptions;

/// <summary>
/// Enumeration of standard error types used across the application
/// </summary>
public enum ErrorType
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
    public static string ToStringValue(this ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.ValidationError => "validation_error",
            ErrorType.NotFound => "not_found",
            ErrorType.Unauthorized => "unauthorized",
            ErrorType.Forbidden => "forbidden",
            ErrorType.Conflict => "conflict",
            ErrorType.InternalServerError => "internal_server_error",
            ErrorType.BadRequest => "bad_request",
            ErrorType.ServiceUnavailable => "service_unavailable",
            ErrorType.FileTooLarge => "file_too_large",
            ErrorType.UnsupportedFileType => "unsupported_file_type",
            ErrorType.StorageError => "storage_error",
            ErrorType.DatabaseError => "database_error",
            _ => "unknown_error"
        };
    }
}
