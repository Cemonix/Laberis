using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.Common;
using server.Models.DTOs.LabelScheme;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize(Policy = "RequireAuthenticatedUser")]
[EnableRateLimiting("project")]
public class LabelSchemesController : ControllerBase
{
    private readonly ILabelSchemeService _labelSchemeService;

    public LabelSchemesController(ILabelSchemeService labelSchemeService)
    {
        _labelSchemeService = labelSchemeService;
    }

    /// <summary>
    /// Gets all label schemes for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="filterOn">The field to filter on.</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of label schemes.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LabelSchemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLabelSchemesForProject(
        int projectId,
        [FromQuery] string? filterOn = null, [FromQuery] string? filterQuery = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
    {
        var schemes = await _labelSchemeService.GetLabelSchemesForProjectAsync(
            projectId,
            filterOn,
            filterQuery,
            sortBy,
            isAscending,
            pageNumber,
            pageSize
        );
        return Ok(schemes);
    }

    /// <summary>
    /// Gets a single label scheme by its ID within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme.</param>
    /// <returns>The requested label scheme.</returns>
    [HttpGet("{schemeId:int}")]
    public async Task<IActionResult> GetLabelScheme(int projectId, int schemeId)
    {
        var scheme = await _labelSchemeService.GetLabelSchemeByIdAsync(projectId, schemeId);
        if (scheme == null)
        {
            return NotFound();
        }
        return Ok(scheme);
    }

    /// <summary>
    /// Creates a new label scheme within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="createDto">The data for the new label scheme.</param>
    /// <returns>The created label scheme.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateLabelScheme(int projectId, [FromBody] CreateLabelSchemeDto createDto)
    {
        var newScheme = await _labelSchemeService.CreateLabelSchemeAsync(projectId, createDto);
        if (newScheme == null)
        {
            return BadRequest("Failed to create the label scheme. Ensure the project exists and the scheme name is unique for this project.");
        }
        return CreatedAtAction(nameof(GetLabelScheme), new { projectId = newScheme.ProjectId, schemeId = newScheme.Id }, newScheme);
    }

    /// <summary>
    /// Updates an existing label scheme.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme to update.</param>
    /// <param name="updateDto">The updated data for the label scheme.</param>
    /// <returns>The updated label scheme.</returns>
    [HttpPut("{schemeId:int}")]
    public async Task<IActionResult> UpdateLabelScheme(int projectId, int schemeId, [FromBody] UpdateLabelSchemeDto updateDto)
    {
        var updatedScheme = await _labelSchemeService.UpdateLabelSchemeAsync(projectId, schemeId, updateDto);
        if (updatedScheme == null)
        {
            return NotFound();
        }
        return Ok(updatedScheme);
    }

    /// <summary>
    /// Deletes a label scheme and all associated labels.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme to delete.</param>
    [HttpDelete("{schemeId:int}")]
    [Authorize(Policy = "RequireAdminRole")] // Example: Only admins can delete schemes
    public async Task<IActionResult> DeleteLabelScheme(int projectId, int schemeId)
    {
        var success = await _labelSchemeService.DeleteLabelSchemeAsync(projectId, schemeId);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
}