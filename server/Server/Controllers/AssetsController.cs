using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Extensions;
using server.Models.DTOs.Asset;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize(Policy = "RequireAuthenticatedUser")]
[ProjectAccess]
[EnableRateLimiting("public")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetService assetService, ILogger<AssetsController> logger)
    {
        _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all assets for a specific project with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="projectId">The ID of the project to get assets for.</param>
    /// <param name="dataSourceId">Optional ID to filter assets by data source.</param>
    /// <param name="filterOn">The field to filter on (e.g., "filename", "mime_type", "status").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "filename", "created_at", "size_bytes").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of asset DTOs.</returns>
    /// <response code="200">Returns the list of asset DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [Authorize(Policy = "CanAccessDataExplorer")]  // Viewer and Manager only
    [ProducesResponseType(typeof(IEnumerable<AssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAssetsForProject(
        int projectId,
        [FromQuery] int? dataSourceId = null,
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        var assets = await _assetService.GetAssetsForProjectAsync(
            projectId, dataSourceId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize
        );
        return Ok(assets);
    }

    /// <summary>
    /// Gets a specific asset by its unique ID.
    /// </summary>
    /// <param name="projectId">The ID of the project the asset belongs to.</param>
    /// <param name="assetId">The ID of the asset.</param>
    /// <returns>The requested asset.</returns>
    [HttpGet("{assetId:int}")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetById(int projectId, int assetId)
    {
        var asset = await _assetService.GetAssetByIdAsync(assetId);
        return asset == null ? this.CreateNotFoundResponse("Asset", assetId) : Ok(asset);
    }

    /// <summary>
    /// Creates a new asset for a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the asset for.</param>
    /// <param name="createAssetDto">The asset creation data.</param>
    /// <returns>The newly created asset.</returns>
    /// <response code="201">Returns the newly created asset and its location.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAsset(int projectId, [FromBody] CreateAssetDto createAssetDto)
    {
        var newAsset = await _assetService.CreateAssetAsync(projectId, createAssetDto);
        return CreatedAtAction(nameof(GetAssetById), new { projectId, assetId = newAsset.Id }, newAsset);
    }

    /// <summary>
    /// Updates an existing asset.
    /// </summary>
    /// <param name="assetId">The ID of the asset to update.</param>
    /// <param name="updateAssetDto">The data to update the asset with.</param>
    /// <returns>The updated asset.</returns>
    /// <response code="200">Returns the updated asset.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the asset is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{assetId:int}")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAsset(int assetId, [FromBody] UpdateAssetDto updateAssetDto)
    {
        var updatedAsset = await _assetService.UpdateAssetAsync(assetId, updateAssetDto);
        return updatedAsset == null ? this.CreateNotFoundResponse("Asset", assetId) : Ok(updatedAsset);
    }

    /// <summary>
    /// Deletes an asset by its ID.
    /// </summary>
    /// <param name="assetId">The ID of the asset to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the asset was successfully deleted.</response>
    /// <response code="404">If the asset is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{assetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsset(int assetId)
    {
        var result = await _assetService.DeleteAssetAsync(assetId);
        return !result ? this.CreateNotFoundResponse("Asset", assetId) : NoContent();
    }

    /// <summary>
    /// Uploads a single asset file.
    /// </summary>
    /// <param name="projectId">The ID of the project to upload the asset to.</param>
    /// <param name="file">The file to upload.</param>
    /// <param name="dataSourceId">The data source ID to associate with this asset.</param>
    /// <param name="metadata">Optional metadata for the asset as JSON string.</param>
    /// <returns>The upload result.</returns>
    /// <response code="200">Returns the upload result.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("upload")]
    [EnableRateLimiting("upload")]
    [ProducesResponseType(typeof(UploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAsset(
        int projectId,
        [FromForm] IFormFile file,
        [FromForm] int dataSourceId,
        [FromForm] string? metadata = null)
    {
        // Validate file is provided
        if (file == null)
        {
            return BadRequest("No file provided for upload");
        }

        var uploadDto = new UploadAssetDto
        {
            File = file,
            DataSourceId = dataSourceId,
            Metadata = metadata
        };

        var result = await _assetService.UploadAssetAsync(projectId, uploadDto);
        return Ok(result);
    }

    /// <summary>
    /// Uploads multiple asset files in bulk.
    /// </summary>
    /// <param name="projectId">The ID of the project to upload the assets to.</param>
    /// <param name="files">The files to upload.</param>
    /// <param name="dataSourceId">The data source ID to associate with these assets.</param>
    /// <param name="metadata">Optional metadata for all assets as JSON string.</param>
    /// <returns>The bulk upload result.</returns>
    /// <response code="200">Returns the bulk upload result.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("upload/bulk")]
    [EnableRateLimiting("upload")]
    [ProducesResponseType(typeof(BulkUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAssets(
        int projectId,
        [FromForm] IFormFileCollection files,
        [FromForm] int dataSourceId,
        [FromForm] string? metadata = null)
    {
        // Validate files collection
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided for upload");
        }

        var bulkUploadDto = new BulkUploadAssetDto
        {
            Files = files,
            DataSourceId = dataSourceId,
            Metadata = metadata
        };

        var result = await _assetService.UploadAssetsAsync(projectId, bulkUploadDto);
        return Ok(result);
    }
}
