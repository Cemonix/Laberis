using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.Issue;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(IIssueService issueService, ILogger<IssuesController> logger)
    {
        _issueService = issueService ?? throw new ArgumentNullException(nameof(issueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all issues for a specific asset with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="assetId">The ID of the asset to get issues for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "status", "priority", "issue_type", "assigned_to_user_id").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "priority", "status").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of issue DTOs.</returns>
    /// <response code="200">Returns the list of issue DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("asset/{assetId:int}")]
    [ProducesResponseType(typeof(IEnumerable<IssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIssuesForAsset(
        int assetId,
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
            var issues = await _issueService.GetIssuesForAssetAsync(
                assetId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching issues for asset {AssetId}.", assetId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all issues assigned to a specific user with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="userId">The ID of the user to get assigned issues for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "status", "priority", "issue_type").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "priority", "status").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of issue DTOs.</returns>
    /// <response code="200">Returns the list of issue DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<IssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIssuesForUser(
        string userId,
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
            var issues = await _issueService.GetIssuesForUserAsync(
                userId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching issues for user {UserId}.", userId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all issues assigned to the current user with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="filterOn">The field to filter on (e.g., "status", "priority", "issue_type").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "priority", "status").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of issue DTOs.</returns>
    /// <response code="200">Returns the list of issue DTOs.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("my-issues")]
    [ProducesResponseType(typeof(IEnumerable<IssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyIssues(
        [FromQuery] string? filterOn = null,
        [FromQuery] string? filterQuery = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var issues = await _issueService.GetIssuesForUserAsync(
                userId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching issues for current user {UserId}.", userId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific issue by its unique ID.
    /// </summary>
    /// <param name="issueId">The ID of the issue.</param>
    /// <returns>The requested issue.</returns>
    [HttpGet("{issueId:int}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueById(int issueId)
    {
        try
        {
            var issue = await _issueService.GetIssueByIdAsync(issueId);

            if (issue == null)
            {
                return NotFound($"Issue with ID {issueId} not found.");
            }

            return Ok(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching issue {IssueId}.", issueId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new issue.
    /// </summary>
    /// <param name="createIssueDto">The issue creation data.</param>
    /// <returns>The newly created issue.</returns>
    /// <response code="201">Returns the newly created issue and its location.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateIssue([FromBody] CreateIssueDto createIssueDto)
    {
        var reportedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(reportedByUserId))
        {
            return Unauthorized("User ID claim not found in token.");
        }

        try
        {
            var newIssue = await _issueService.CreateIssueAsync(createIssueDto, reportedByUserId);
            return CreatedAtAction(nameof(GetIssueById), 
                new { issueId = newIssue.Id }, newIssue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating issue for asset {AssetId}.", createIssueDto.AssetId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Updates an existing issue.
    /// </summary>
    /// <param name="issueId">The ID of the issue to update.</param>
    /// <param name="updateIssueDto">The data to update the issue with.</param>
    /// <returns>The updated issue.</returns>
    /// <response code="200">Returns the updated issue.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the issue is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{issueId:int}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateIssue(int issueId, [FromBody] UpdateIssueDto updateIssueDto)
    {
        try
        {
            var updatedIssue = await _issueService.UpdateIssueAsync(issueId, updateIssueDto);

            if (updatedIssue == null)
            {
                return NotFound($"Issue with ID {issueId} not found.");
            }

            return Ok(updatedIssue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating issue {IssueId}.", issueId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes an issue by its ID.
    /// </summary>
    /// <param name="issueId">The ID of the issue to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the issue was successfully deleted.</response>
    /// <response code="404">If the issue is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{issueId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteIssue(int issueId)
    {
        try
        {
            var result = await _issueService.DeleteIssueAsync(issueId);

            if (!result)
            {
                return NotFound($"Issue with ID {issueId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting issue {IssueId}.", issueId);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }
}
