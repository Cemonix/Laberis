namespace server.Exceptions;

/// <summary>
/// Exception thrown when database operations fail
/// </summary>
public class DatabaseException : AppException
{
    public override int StatusCode => 500;
    public override ErrorType ErrorType => ErrorType.DatabaseError;

    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
}
