using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using server.Exceptions;
using server.Services.Interfaces;

namespace server.Services.Storage;

/// <summary>
/// MinIO implementation of file storage service
/// </summary>
public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IMinioClient minioClient, ILogger<MinioFileStorageService> logger)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadFileAsync(Stream file, string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading file to MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            await EnsureBucketExistsAsync(bucketName, cancellationToken);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(file)
                .WithObjectSize(file.Length);

            var response = await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
            
            _logger.LogInformation("Successfully uploaded file to MinIO: {BucketName}/{ObjectName}", bucketName, objectName);
            return $"{bucketName}/{objectName}";
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to upload file to MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Failed to upload file to storage: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading file to MinIO");
            throw new StorageException($"Unexpected error during file upload: {ex.Message}", ex);
        }
    }

    public async Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading file from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogInformation("Successfully downloaded file from MinIO: {BucketName}/{ObjectName}", bucketName, objectName);
            return memoryStream;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to download file from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Failed to download file from storage: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while downloading file from MinIO");
            throw new StorageException($"Unexpected error during file download: {ex.Message}", ex);
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting file from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation("Successfully deleted file from MinIO: {BucketName}/{ObjectName}", bucketName, objectName);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to delete file from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Failed to delete file from storage: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting file from MinIO");
            throw new StorageException($"Unexpected error during file deletion: {ex.Message}", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if file exists in MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogDebug("File does not exist in MinIO: {BucketName}/{ObjectName}", bucketName, objectName);
            return false;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Error checking if file exists in MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Failed to check file existence: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking file existence in MinIO");
            throw new StorageException($"Unexpected error during file existence check: {ex.Message}", ex);
        }
    }

    public async Task<FileMetadata> GetFileMetadataAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting file metadata from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            return new FileMetadata
            {
                Size = objectStat.Size,
                ContentType = objectStat.ContentType ?? "application/octet-stream",
                LastModified = objectStat.LastModified,
                ETag = objectStat.ETag ?? string.Empty,
                Metadata = objectStat.MetaData?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>()
            };
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogWarning("File metadata not found in MinIO: {BucketName}/{ObjectName}", bucketName, objectName);
            throw new NotFoundException($"File not found: {bucketName}/{objectName}");
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to get file metadata from MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Failed to get file metadata: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting file metadata from MinIO");
            throw new StorageException($"Unexpected error during metadata retrieval: {ex.Message}", ex);
        }
    }

    public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!bucketExists)
            {
                _logger.LogInformation("Creating MinIO bucket '{BucketName}'", bucketName);

                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

                _logger.LogInformation("Successfully created MinIO bucket '{BucketName}'", bucketName);
            }
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to ensure MinIO bucket '{BucketName}' exists", bucketName);
            throw new StorageException($"Failed to ensure bucket exists: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while ensuring MinIO bucket exists");
            throw new StorageException($"Unexpected error during bucket creation: {ex.Message}", ex);
        }
    }
}
