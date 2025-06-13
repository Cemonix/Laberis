using server.Models.DTOs.Asset;

namespace server.Services.Interfaces;

public interface IAssetService
{
    /// <summary>
    /// Retrieves all assets for a specific project, optionally filtered and sorted.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve assets for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "filename", "status", "mime_type").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "filename", "created_at", "size_bytes").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of AssetDto.</returns>
    Task<IEnumerable<AssetDto>> GetAssetsForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
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
}
