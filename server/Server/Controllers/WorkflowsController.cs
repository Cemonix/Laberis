using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.Workflow;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(IWorkflowService workflowService, ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all workflows for a specific project with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="projectId">The ID of the project to get workflows for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "name", "is_active").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "name", "created_at").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of workflow DTOs.</returns>
    /// <response code="200">Returns the list of workflow DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowsForProject(
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
            var workflows = await _workflowService.GetWorkflowsForProjectAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching workflows for project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific workflow by its unique ID.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>The requested workflow.</returns>
    [HttpGet("{workflowId:int}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowById(int workflowId)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(workflowId);

            if (workflow == null)
            {
                return NotFound($"Workflow with ID {workflowId} not found.");
            }

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new workflow for a project.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the workflow for.</param>
    /// <param name="createWorkflowDto">The workflow creation data.</param>
    /// <returns>The created workflow.</returns>
    /// <response code="201">Returns the newly created workflow.</response>
    /// <response code="400">If the workflow data is invalid.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWorkflow(int projectId, [FromBody] CreateWorkflowDto createWorkflowDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdWorkflow = await _workflowService.CreateWorkflowAsync(projectId, createWorkflowDto);
            
            return CreatedAtAction(
                nameof(GetWorkflowById),
                new { projectId, workflowId = createdWorkflow.Id },
                createdWorkflow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating workflow for project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Updates an existing workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to update.</param>
    /// <param name="updateWorkflowDto">The workflow update data.</param>
    /// <returns>The updated workflow.</returns>
    /// <response code="200">Returns the updated workflow.</response>
    /// <response code="400">If the workflow data is invalid.</response>
    /// <response code="404">If the workflow is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPut("{workflowId:int}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateWorkflow(int workflowId, [FromBody] UpdateWorkflowDto updateWorkflowDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedWorkflow = await _workflowService.UpdateWorkflowAsync(workflowId, updateWorkflowDto);

            if (updatedWorkflow == null)
            {
                return NotFound($"Workflow with ID {workflowId} not found.");
            }

            return Ok(updatedWorkflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Deletes a workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to delete.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="404">If the workflow is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpDelete("{workflowId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteWorkflow(int workflowId)
    {
        try
        {
            var result = await _workflowService.DeleteWorkflowAsync(workflowId);

            if (!result)
            {
                return NotFound($"Workflow with ID {workflowId} not found.");
            }

            return Ok(new { message = "Workflow deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
