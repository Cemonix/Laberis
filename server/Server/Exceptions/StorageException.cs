namespace server.Exceptions;

/// <summary>
/// Exception thrown when storage operations fail
/// </summary>
public class StorageException : AppException
{
    public override int StatusCode => 500;
    public override ErrorType ErrorType => ErrorType.StorageError;

    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}
