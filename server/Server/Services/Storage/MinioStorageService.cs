using System;
using System.Text.RegularExpressions;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using server.Exceptions;
using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace server.Services.Storage;

public class MinioStorageService : IStorageService, IFileStorageService
{
    private readonly ILogger<MinioStorageService> _logger;
    private readonly IMinioClient _minioClient;
    private const int MaxBucketNameLength = 50;

    public DataSourceType ForType => DataSourceType.MINIO_BUCKET;

    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _logger = logger;
        _minioClient = minioClient;
    }

    #region Bucket Methods Region

    public string GenerateBucketName(int projectId, string dataSourceName = "default")
    {
        var sanitizedName = SanitizeBucketName(dataSourceName);
        return $"{sanitizedName}-{projectId}";
    }

    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        // Defensive sanitization - ensure bucket name is always valid
        bucketName = SanitizeBucketName(bucketName);
        
        _logger.LogInformation("Checking if Minio bucket '{BucketName}' exists.", bucketName);
        var args = new BucketExistsArgs().WithBucket(bucketName);
        try
        {
            return await _minioClient.BucketExistsAsync(args, cancellationToken);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "An error occurred while checking for bucket '{BucketName}'.", bucketName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing all Minio buckets.");
        try
        {
            var buckets = await _minioClient.ListBucketsAsync(cancellationToken);
            if (buckets.Buckets == null)
            {
                throw new InvalidOperationException("No buckets found or Minio client is not configured correctly.");
            }
            return buckets.Buckets.Select(b => b.Name);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "An error occurred while listing Minio buckets.");
            throw;
        }
    }

    public async Task CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {

        bucketName = SanitizeBucketName(bucketName);
        if (bucketName.Length > MaxBucketNameLength)
        {
            _logger.LogWarning("Bucket name '{BucketName}' exceeds maximum length of {MaxLength}. Trimming to fit.", bucketName, MaxBucketNameLength);
            bucketName = bucketName[..MaxBucketNameLength];
        }
        

        if (await BucketExistsAsync(bucketName, cancellationToken))
        {
            _logger.LogWarning("Minio bucket '{BucketName}' already exists. Skipping creation.", bucketName);
        }

        _logger.LogInformation("Creating new Minio bucket '{BucketName}'.", bucketName);

        var args = new MakeBucketArgs().WithBucket(bucketName);
        try
        {
            await _minioClient.MakeBucketAsync(args, cancellationToken);
            _logger.LogInformation("Successfully created Minio bucket '{BucketName}'.", bucketName);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to create Minio bucket '{BucketName}'.", bucketName);
            throw;
        }
    }

    public async Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        bucketName = SanitizeBucketName(bucketName);
        
        if (!await BucketExistsAsync(bucketName, cancellationToken))
        {
            _logger.LogWarning("Minio bucket '{BucketName}' does not exist. Skipping deletion.", bucketName);
            return;
        }

        _logger.LogInformation("Deleting Minio bucket '{BucketName}'.", bucketName);
        var args = new RemoveBucketArgs().WithBucket(bucketName);
        try
        {
            await _minioClient.RemoveBucketAsync(args, cancellationToken);
            _logger.LogInformation("Successfully deleted Minio bucket '{BucketName}'.", bucketName);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to delete Minio bucket '{BucketName}'.", bucketName);
            throw;
        }
    }

    #endregion

    #region File Storage Methods

    public async Task<string> UploadFileAsync(Stream file, string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading file to MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);

            if (!await BucketExistsAsync(bucketName, cancellationToken))
            {
                _logger.LogInformation("Bucket {BucketName} does not exist, creating it", bucketName);
                await CreateBucketAsync(bucketName, cancellationToken);
            }

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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while uploading file to MinIO bucket '{BucketName}' with object name '{ObjectName}'", bucketName, objectName);
            throw new StorageException($"Invalid operation during file upload: {ex.Message}", ex);
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

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating presigned URL for MinIO object '{ObjectName}' in bucket '{BucketName}' with expiry {ExpirySeconds}s", objectName, bucketName, expiryInSeconds);

            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds);

            var presignedUrl = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogDebug("Generated presigned URL for {BucketName}/{ObjectName}", bucketName, objectName);
            return presignedUrl;
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogWarning("Object not found for presigned URL generation: {BucketName}/{ObjectName}", bucketName, objectName);
            throw new NotFoundException($"Object not found: {bucketName}/{objectName}");
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for MinIO object '{ObjectName}' in bucket '{BucketName}'", objectName, bucketName);
            throw new StorageException($"Failed to generate presigned URL: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating presigned URL");
            throw new StorageException($"Unexpected error during presigned URL generation: {ex.Message}", ex);
        }
    }

    #endregion
    
    /// <summary>
    /// Sanitizes a string to be used as part of a bucket name
    /// Converts to lowercase, replaces invalid characters with hyphens
    /// </summary>
    private static string SanitizeBucketName(string name)
    {
        // Convert to lowercase and replace invalid characters with hyphens
        var sanitized = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any characters that aren't letters, numbers, or hyphens
        // TODO: Use GeneratedRegexAttribute
        sanitized = Regex.Replace(sanitized, "[^a-z0-9-]", "");

        // Ensure it doesn't start or end with hyphen
        sanitized = sanitized.Trim('-');

        // Ensure it's not empty after sanitization and limit length to avoid bucket name issues
        if (string.IsNullOrEmpty(sanitized))
            // TODO: Create a custom exception for this
            throw new ArgumentException("Bucket name cannot be empty or consist only of invalid characters.");

        // MinIO bucket names have length restrictions
        if (sanitized.Length > MaxBucketNameLength)
            sanitized = sanitized[..MaxBucketNameLength].TrimEnd('-');

        return sanitized;
    }
}
