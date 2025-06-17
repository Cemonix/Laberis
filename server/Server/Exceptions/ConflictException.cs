namespace server.Exceptions;

/// <summary>
/// Exception thrown when a business rule conflict occurs
/// </summary>
public class ConflictException : AppException
{
    public override int StatusCode => 409;
    public override ErrorTypes ErrorType => ErrorTypes.Conflict;

    public ConflictException(string message) : base(message) { }
}
