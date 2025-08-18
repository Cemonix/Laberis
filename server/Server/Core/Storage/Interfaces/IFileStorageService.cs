namespace server.Services.Interfaces;

/// <summary>
/// Service for handling file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the storage path
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="bucketName">The bucket/container name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The storage path of the uploaded file</returns>
    Task<string> UploadFileAsync(Stream file, string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage
    /// </summary>
    /// <param name="bucketName">The bucket/container name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The file stream</returns>
    Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="bucketName">The bucket/container name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    /// <param name="bucketName">The bucket/container name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file exists</returns>
    Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata from storage
    /// </summary>
    /// <param name="bucketName">The bucket/container name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata including size, content type, etc.</returns>
    Task<FileMetadata> GetFileMetadataAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from one bucket to another (copy + delete source)
    /// </summary>
    /// <param name="sourceBucketName">The source bucket name</param>
    /// <param name="targetBucketName">The target bucket name</param>
    /// <param name="objectName">The object name (path) in storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The storage path of the moved file</returns>
    Task<string> MoveFileAsync(string sourceBucketName, string targetBucketName, string objectName, CancellationToken cancellationToken = default);
}


// TODO: Move to a separate file
/// <summary>
/// Represents file metadata from storage
/// </summary>
public record FileMetadata
{
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public string ETag { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
}
