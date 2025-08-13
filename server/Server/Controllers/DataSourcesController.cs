using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Authentication;
using server.Models.DTOs.DataSource;
using server.Models.DTOs.WorkflowStage;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/datasources")]
[ApiController]
[Authorize(Policy = "RequireAuthenticatedUser")]
[ProjectAccess]
[EnableRateLimiting("project")]
public class DataSourcesController : ControllerBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly IWorkflowStageService _workflowStageService;

    public DataSourcesController(IDataSourceService dataSourceService, IWorkflowStageService workflowStageService)
    {
        _dataSourceService = dataSourceService;
        _workflowStageService = workflowStageService;
    }

    /// <summary>
    /// Gets all data sources for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="filterOn">The field to filter on.</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of data sources.</returns>
    [HttpGet]
    [Authorize(Policy = "CanManageDataSources")]  // Manager only
    public async Task<IActionResult> GetAllDataSourcesForProject(
        int projectId,
        [FromQuery] string? filterOn = null, [FromQuery] string? filterQuery = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool isAscending = true, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
    {
        var dataSources = await _dataSourceService.GetAllDataSourcesForProjectAsync(
            projectId,
            filterOn,
            filterQuery,
            sortBy,
            isAscending,
            pageNumber,
            pageSize
        );
        return Ok(dataSources);
    }

    /// <summary>
    /// Gets a single data source by its ID within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>The requested data source.</returns>
    [HttpGet("{dataSourceId:int}")]
    [Authorize(Policy = "CanManageDataSources")]  // Manager only
    public async Task<IActionResult> GetDataSourceById(int projectId, int dataSourceId)
    {
        var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
        if (dataSource == null || dataSource.ProjectId != projectId)
        {
            return NotFound();
        }
        return Ok(dataSource);
    }

    /// <summary>
    /// Creates a new data source within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="createDto">The data for the new data source.</param>
    /// <returns>The created data source.</returns>
    [HttpPost]
    [Authorize(Policy = "CanManageDataSources")]  // Manager only
    public async Task<IActionResult> CreateDataSource(int projectId, [FromBody] CreateDataSourceDto createDto)
    {
        var newDataSource = await _dataSourceService.CreateDataSourceAsync(projectId, createDto);
        if (newDataSource == null)
        {
            return BadRequest("Failed to create the data source. Ensure the project exists.");
        }
        return CreatedAtAction(nameof(GetDataSourceById), new { projectId, dataSourceId = newDataSource.Id }, newDataSource);
    }

    /// <summary>
    /// Updates an existing data source within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <param name="updateDto">The data for updating the data source.</param>
    /// /// <returns>The updated data source.</returns>
    [HttpPut("{dataSourceId:int}")]
    [Authorize(Policy = "CanManageDataSources")]  // Manager only
    public async Task<IActionResult> UpdateDataSource(int projectId, int dataSourceId, [FromBody] UpdateDataSourceDto updateDto)
    {
        var updatedDataSource = await _dataSourceService.UpdateDataSourceAsync(dataSourceId, updateDto);
        if (updatedDataSource == null || updatedDataSource.ProjectId != projectId)
        {
            return NotFound();
        }
        return Ok(updatedDataSource);
    }

    /// <summary>
    /// Deletes a data source from a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source to delete.</param>
    [HttpDelete("{dataSourceId:int}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteDataSource(int projectId, int dataSourceId)
    {
        var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
        if (dataSource == null || dataSource.ProjectId != projectId)
        {
            return NotFound();
        }

        var success = await _dataSourceService.DeleteDataSourceAsync(dataSourceId);
        if (!success)
        {
            // This case should ideally not be hit if the above check passes, but is good for safety.
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Checks if a data source is already being used by other workflow stages.
    /// This prevents data source conflicts when creating new workflows.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source to check.</param>
    /// <param name="excludeWorkflowId">Optional workflow ID to exclude from the check (for updates).</param>
    /// <returns>A list of conflicting workflow stages using the data source.</returns>
    [HttpGet("{dataSourceId:int}/conflicts")]
    public async Task<IActionResult> GetDataSourceUsageConflicts(
        int projectId, 
        int dataSourceId, 
        [FromQuery] int? excludeWorkflowId = null)
    {
        try
        {
            // Validate that the data source belongs to the project
            var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
            if (dataSource == null || dataSource.ProjectId != projectId)
            {
                return NotFound($"Data source {dataSourceId} not found in project {projectId}.");
            }

            var conflicts = await _workflowStageService.GetDataSourceUsageConflictsAsync(dataSourceId, excludeWorkflowId);
            return Ok(conflicts);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An unexpected error occurred while checking data source conflicts.");
        }
    }

    /// <summary>
    /// Gets all available data source types that are configured and ready to use.
    /// </summary>
    /// <returns>A list of available data source types.</returns>
    [HttpGet("types/available")]
    public async Task<IActionResult> GetAvailableDataSourceTypes()
    {
        try
        {
            var availableTypes = await _dataSourceService.GetAvailableDataSourceTypesAsync();
            return Ok(availableTypes);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve available data source types");
        }
    }
}
