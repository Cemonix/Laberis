namespace server.Exceptions;

/// <summary>
/// Exception thrown when file operations fail
/// </summary>
public class FileOperationException : AppException
{
    public override int StatusCode => 400;
    public override ErrorType ErrorType { get; }
    public override Dictionary<string, object>? Metadata { get; }

    public FileOperationException(ErrorType errorType, string message, Dictionary<string, object>? metadata = null) 
        : base(message)
    {
        ErrorType = errorType;
        Metadata = metadata;
    }

    public static FileOperationException FileTooLarge(string fileName, long maxSize)
    {
        return new FileOperationException(
            ErrorType.FileTooLarge,
            $"File '{fileName}' exceeds the maximum allowed size of {maxSize} bytes.",
            new Dictionary<string, object> { ["fileName"] = fileName, ["maxSize"] = maxSize }
        );
    }

    public static FileOperationException UnsupportedFileType(string fileName, string fileType)
    {
        return new FileOperationException(
            ErrorType.UnsupportedFileType,
            $"File type '{fileType}' is not supported for file '{fileName}'.",
            new Dictionary<string, object> { ["fileName"] = fileName, ["fileType"] = fileType }
        );
    }
}
