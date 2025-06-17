namespace server.Exceptions;

/// <summary>
/// Base exception for all application-specific exceptions
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public abstract ErrorTypes ErrorType { get; }
    public virtual Dictionary<string, object>? Metadata { get; }

    protected AppException(string message) : base(message) { }
    protected AppException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Gets the string representation of the error type
    /// </summary>
    public string ErrorTypeString => ErrorType.ToStringValue();
}
