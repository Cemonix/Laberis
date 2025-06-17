namespace server.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : AppException
{
    public override int StatusCode => 400;
    public override ErrorTypes ErrorType => ErrorTypes.ValidationError;
    public Dictionary<string, List<string>> ValidationErrors { get; }

    public ValidationException(string message) : base(message)
    {
        ValidationErrors = new Dictionary<string, List<string>>();
    }

    public ValidationException(Dictionary<string, List<string>> validationErrors) 
        : base("One or more validation errors occurred.")
    {
        ValidationErrors = validationErrors;
    }
}
