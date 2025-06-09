using System;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using server.Configs;
using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace server.Services.Storage;

public class MinioStorageService : IStorageService
{
    private readonly ILogger<MinioStorageService> _logger;
    private readonly IMinioClient _minioClient;

    public DataSourceType ForType => DataSourceType.MINIO_BUCKET;

    public MinioStorageService(IOptions<MinioSettings> minioSettings, ILogger<MinioStorageService> logger)
    {
        _logger = logger;
        var settings = minioSettings.Value;

        _minioClient = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl)
            .Build();
    }

    public async Task<bool> ContainerExistsAsync(string containerName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if Minio bucket '{BucketName}' exists.", containerName);
        var args = new BucketExistsArgs().WithBucket(containerName);
        try
        {
            return await _minioClient.BucketExistsAsync(args, cancellationToken);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "An error occurred while checking for bucket '{BucketName}'.", containerName);
            throw;
        }
    }

    public async Task CreateContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        if (await ContainerExistsAsync(containerName, cancellationToken))
        {
            _logger.LogWarning("Minio bucket '{BucketName}' already exists. Skipping creation.", containerName);
            return;
        }

        _logger.LogInformation("Creating new Minio bucket '{BucketName}'.", containerName);
        var args = new MakeBucketArgs().WithBucket(containerName);
        try
        {
            await _minioClient.MakeBucketAsync(args, cancellationToken);
            _logger.LogInformation("Successfully created Minio bucket '{BucketName}'.", containerName);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to create Minio bucket '{BucketName}'.", containerName);
            throw;
        }
    }
}
