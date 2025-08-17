using server.Models.Common;
using server.Models.DTOs.Asset;
using server.Models.Domain;
using server.Models.Domain.Enums;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;
using server.Models.Internal;

namespace server.Services.Interfaces;

public interface IAssetService
{
    /// <summary>
    /// Retrieves all assets for a specific project, optionally filtered and sorted.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve assets for.</param>
    /// <param name="dataSourceId">Optional ID to filter assets by data source.</param>
    /// <param name="filterOn">The field to filter on (e.g., "filename", "status", "mime_type").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "filename", "created_at", "size_bytes").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="includeUrls">Whether to include presigned URLs in the response (default: true).</param>
    /// <param name="urlExpiryInSeconds">The expiry time for URLs in seconds (default: 1 hour).</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of AssetDto.</returns>
    Task<PaginatedResponse<AssetDto>> GetAssetsForProjectAsync(
        int projectId,
        int? dataSourceId = null,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25,
        bool includeUrls = true, int urlExpiryInSeconds = 3600
    );

    /// <summary>
    /// Retrieves an asset by its ID.
    /// </summary>
    /// <param name="assetId">The ID of the asset to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the AssetDto if found, otherwise null.</returns>
    Task<AssetDto?> GetAssetByIdAsync(int assetId);

    /// <summary>
    /// Creates a new asset.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the asset in.</param>
    /// <param name="createDto">The DTO containing information for the new asset.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created AssetDto.</returns>
    Task<AssetDto> CreateAssetAsync(int projectId, CreateAssetDto createDto);

    /// <summary>
    /// Updates an existing asset.
    /// </summary>
    /// <param name="assetId">The ID of the asset to update.</param>
    /// <param name="updateDto">The DTO containing updated asset information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated AssetDto if successful, otherwise null.</returns>
    Task<AssetDto?> UpdateAssetAsync(int assetId, UpdateAssetDto updateDto);

    /// <summary>
    /// Deletes an asset by its ID.
    /// </summary>
    /// <param name="assetId">The ID of the asset to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the asset was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteAssetAsync(int assetId);

    /// <summary>
    /// Uploads a single asset file.
    /// </summary>
    /// <param name="projectId">The ID of the project to upload the asset to.</param>
    /// <param name="uploadDto">The upload DTO containing the file and metadata.</param>
    /// <returns>A task that represents the asynchronous operation, containing the upload result.</returns>
    Task<UploadResultDto> UploadAssetAsync(int projectId, UploadAssetDto uploadDto);

    /// <summary>
    /// Uploads multiple asset files in bulk.
    /// </summary>
    /// <param name="projectId">The ID of the project to upload the assets to.</param>
    /// <param name="bulkUploadDto">The bulk upload DTO containing the files and metadata.</param>
    /// <returns>A task that represents the asynchronous operation, containing the bulk upload result.</returns>
    Task<BulkUploadResultDto> UploadAssetsAsync(int projectId, BulkUploadAssetDto bulkUploadDto);

    /// <summary>
    /// Validates that an asset belongs to a specific project.
    /// </summary>
    /// <param name="assetId">The ID of the asset.</param>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the asset belongs to the project.</returns>
    Task<bool> ValidateAssetBelongsToProjectAsync(int assetId, int projectId);

    /// <summary>
    /// Handles asset movement as part of task workflow progression.
    /// </summary>
    /// <param name="task">The task that is being completed or status changed.</param>
    /// <param name="targetStatus">The target status the task is changing to.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task that represents the asynchronous operation, containing information about the movement.</returns>
    Task<AssetMovementResult> HandleTaskWorkflowAssetMovementAsync(Models.Domain.Task task, TaskStatus targetStatus, string userId);

    /// <summary>
    /// Handles asset movement back to annotation stage when a task is vetoed/returned for rework.
    /// </summary>
    /// <param name="task">The task that is being vetoed.</param>
    /// <param name="userId">The ID of the user performing the veto action.</param>
    /// <returns>A task that represents the asynchronous operation, containing information about the movement.</returns>
    Task<AssetMovementResult> HandleTaskVetoAssetMovementAsync(Models.Domain.Task task, string userId);

    /// <summary>
    /// Checks if a data source has assets available for task creation.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if assets are available.</returns>
    Task<bool> HasAssetsAvailableAsync(int projectId);

    /// <summary>
    /// Gets the count of available assets for task creation in a specific data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task returning the count of available assets.</returns>
    Task<int> GetAvailableAssetsCountAsync(int projectId);

    /// <summary>
    /// Gets the count of available assets for task creation in a specific data source.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>A task returning the list of available assets.</returns>
    Task<IEnumerable<Asset>> GetAvailableAssetsFromDataSourceAsync(int projectId, int dataSourceId);
}