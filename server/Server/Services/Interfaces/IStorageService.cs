using System;
using server.Models.Domain.Enums;

namespace server.Services.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Gets the type of data source this service supports.
    /// </summary>
    DataSourceType ForType { get; }

    /// <summary>
    /// Generates a bucket name based on project ID and data source name
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="dataSourceName">The data source name</param>
    /// <returns>A properly formatted bucket name</returns>
    string GenerateBucketName(int projectId, string dataSourceName);
    
    /// <summary>
    /// Creates a new storage bucket if it does not already exist.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<string> CreateBucketAsync(string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a storage bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all storage buckets available in the storage service.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns a collection of bucket names.</returns>
    Task<IEnumerable<string>> ListBucketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a storage bucket exists.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the bucket exists, otherwise false.</returns>
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);
}
