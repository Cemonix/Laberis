namespace server.Core.Storage;

/// <summary>
/// Exception thrown when a bucket name cannot be sanitized or is invalid
/// </summary>
public class InvalidBucketNameException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the InvalidBucketNameException class with a default message
    /// </summary>
    public InvalidBucketNameException() 
        : base("Bucket name cannot be empty or consist only of invalid characters.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidBucketNameException class with a specified error message
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    public InvalidBucketNameException(string message) 
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidBucketNameException class with a specified error message and a reference to the inner exception
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public InvalidBucketNameException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}