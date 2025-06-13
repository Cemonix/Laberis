using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.Asset;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
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
    [ProducesResponseType(typeof(IEnumerable<AssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAssetsForProject(
        int projectId,
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        try
        {
            var assets = await _assetService.GetAssetsForProjectAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching assets for project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
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
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId);

            if (asset == null)
            {
                return NotFound($"Asset with ID {assetId} not found.");
            }

            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching asset {AssetId}.", assetId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
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
        try
        {
            var newAsset = await _assetService.CreateAssetAsync(projectId, createAssetDto);
            return CreatedAtAction(nameof(GetAssetById), 
                new { projectId = projectId, assetId = newAsset.Id }, newAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating asset for project {ProjectId}.", projectId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Updates an existing asset.
    /// </summary>
    /// <param name="projectId">The ID of the project the asset belongs to.</param>
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
    public async Task<IActionResult> UpdateAsset(int projectId, int assetId, [FromBody] UpdateAssetDto updateAssetDto)
    {
        try
        {
            var updatedAsset = await _assetService.UpdateAssetAsync(assetId, updateAssetDto);

            if (updatedAsset == null)
            {
                return NotFound($"Asset with ID {assetId} not found.");
            }

            return Ok(updatedAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating asset {AssetId}.", assetId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes an asset by its ID.
    /// </summary>
    /// <param name="projectId">The ID of the project the asset belongs to.</param>
    /// <param name="assetId">The ID of the asset to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the asset was successfully deleted.</response>
    /// <response code="404">If the asset is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{assetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsset(int projectId, int assetId)
    {
        try
        {
            var result = await _assetService.DeleteAssetAsync(assetId);

            if (!result)
            {
                return NotFound($"Asset with ID {assetId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting asset {AssetId}.", assetId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }
}
