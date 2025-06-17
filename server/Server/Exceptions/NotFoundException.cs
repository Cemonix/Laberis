namespace server.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : AppException
{
    public override int StatusCode => 404;
    public override ErrorTypes ErrorType => ErrorTypes.NotFound;

    public NotFoundException(string message) : base(message) { }
    
    public NotFoundException(string resourceType, object id) 
        : base($"{resourceType} with ID '{id}' was not found.") { }
}
