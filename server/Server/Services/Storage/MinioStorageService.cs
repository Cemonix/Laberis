using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using server.Configs;
using server.Exceptions;
using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace server.Services.Storage;

public class MinioStorageService : IStorageService, IFileStorageService
{
    private readonly ILogger<MinioStorageService> _logger;
    private readonly IMinioClient _minioClient;
    private const string DefaultBucketName = "default";
    private const int MaxBucketNameLength = 50;

    public DataSourceType ForType => DataSourceType.MINIO_BUCKET;

    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _logger = logger;
        _minioClient = minioClient;
    }

    #region Bucket Methods Region

    public string GenerateBucketName(int projectId, string dataSourceName)
    {
        var sanitizedName = SanitizeBucketName(dataSourceName);
        return $"{projectId}-{sanitizedName}";
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

    public async Task<string> CreateBucketAsync(string? bucketName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            _logger.LogWarning("Bucket name is null or empty. Using default bucket name '{DefaultBucketName}'.", DefaultBucketName);
            bucketName = DefaultBucketName;
        }
        else
        {
            bucketName = SanitizeBucketName(bucketName);
            if (bucketName.Length > MaxBucketNameLength)
            {
                _logger.LogWarning("Bucket name '{BucketName}' exceeds maximum length of {MaxLength}. Trimming to fit.", bucketName, MaxBucketNameLength);
                bucketName = bucketName[..MaxBucketNameLength];
            }
        }

        if (await BucketExistsAsync(bucketName, cancellationToken))
        {
            _logger.LogWarning("Minio bucket '{BucketName}' already exists. Skipping creation.", bucketName);
            return bucketName;
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

        return bucketName;
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

            await BucketExistsAsync(bucketName, cancellationToken);

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

    #endregion
    
    /// <summary>
    /// Sanitizes a string to be used as part of a bucket name
    /// Converts to lowercase, replaces invalid characters with hyphens
    /// </summary>
    private static string SanitizeBucketName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return DefaultBucketName;

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
            sanitized = DefaultBucketName;

        // MinIO bucket names have length restrictions
        if (sanitized.Length > MaxBucketNameLength)
            sanitized = sanitized[..MaxBucketNameLength].TrimEnd('-');

        return sanitized;
    }
}
