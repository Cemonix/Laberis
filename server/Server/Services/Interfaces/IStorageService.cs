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
    /// Creates a new storage container (e.g., a bucket) if it does not already exist.
    /// </summary>
    /// <param name="containerName">The name of the container to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a storage container exists.
    /// </summary>
    /// <param name="containerName">The name of the container to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the container exists, otherwise false.</returns>
    Task<bool> ContainerExistsAsync(string containerName, CancellationToken cancellationToken = default);
}
