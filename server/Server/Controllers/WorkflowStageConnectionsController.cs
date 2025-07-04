using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.WorkflowStage;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/workflows/{workflowId:int}/connections")]
[ApiController]
[Authorize]
public class WorkflowStageConnectionsController : ControllerBase
{
    private readonly IWorkflowStageConnectionService _connectionService;
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowStageConnectionsController> _logger;

    public WorkflowStageConnectionsController(
        IWorkflowStageConnectionService connectionService,
        IWorkflowService workflowService,
        ILogger<WorkflowStageConnectionsController> logger)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all connections for a specific workflow.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow to get connections for.</param>
    /// <returns>A list of workflow stage connections.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowStageConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowConnections(int projectId, int workflowId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            var connections = await _connectionService.GetConnectionsForWorkflowAsync(workflowId);
            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching connections for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific connection by its ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="connectionId">The ID of the connection.</param>
    /// <returns>The requested connection.</returns>
    [HttpGet("{connectionId:int}")]
    [ProducesResponseType(typeof(WorkflowStageConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConnection(int projectId, int workflowId, int connectionId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
            if (connection == null)
            {
                return NotFound($"Connection with ID {connectionId} not found.");
            }

            // Validate that the connection belongs to the specified workflow
            var isValidConnection = await _connectionService.ValidateConnectionBelongsToWorkflowAsync(connectionId, workflowId);
            if (!isValidConnection)
            {
                return BadRequest($"Connection {connectionId} does not belong to workflow {workflowId}.");
            }

            return Ok(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching connection {ConnectionId}.", connectionId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new connection between workflow stages.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="createDto">The connection creation data.</param>
    /// <returns>The created connection.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowStageConnectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConnection(int projectId, int workflowId, [FromBody] CreateWorkflowStageConnectionDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that both stages belong to the specified workflow
            var areStagesValid = await _connectionService.ValidateConnectionStagesBelongToWorkflowAsync(createDto, workflowId);
            if (!areStagesValid)
            {
                return BadRequest($"One or both stages in the connection do not belong to workflow {workflowId}.");
            }

            var createdConnection = await _connectionService.CreateConnectionAsync(createDto);
            return CreatedAtAction(
                nameof(GetConnection),
                new { projectId, workflowId, connectionId = createdConnection.Id },
                createdConnection
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating connection for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Updates an existing connection.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="connectionId">The ID of the connection to update.</param>
    /// <param name="updateDto">The connection update data.</param>
    /// <returns>The updated connection.</returns>
    [HttpPut("{connectionId:int}")]
    [ProducesResponseType(typeof(WorkflowStageConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConnection(int projectId, int workflowId, int connectionId, [FromBody] UpdateWorkflowStageConnectionDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the connection belongs to the specified workflow
            var isValidConnection = await _connectionService.ValidateConnectionBelongsToWorkflowAsync(connectionId, workflowId);
            if (!isValidConnection)
            {
                return BadRequest($"Connection {connectionId} does not belong to workflow {workflowId}.");
            }

            var updatedConnection = await _connectionService.UpdateConnectionAsync(connectionId, updateDto);
            if (updatedConnection == null)
            {
                return NotFound($"Connection with ID {connectionId} not found.");
            }
            return Ok(updatedConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating connection {ConnectionId}.", connectionId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Deletes a connection.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="connectionId">The ID of the connection to delete.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("{connectionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConnection(int projectId, int workflowId, int connectionId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the connection belongs to the specified workflow
            var isValidConnection = await _connectionService.ValidateConnectionBelongsToWorkflowAsync(connectionId, workflowId);
            if (!isValidConnection)
            {
                return BadRequest($"Connection {connectionId} does not belong to workflow {workflowId}.");
            }

            var result = await _connectionService.DeleteConnectionAsync(connectionId);
            if (!result)
            {
                return NotFound($"Connection with ID {connectionId} not found.");
            }
            return Ok(new { message = "Connection deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting connection {ConnectionId}.", connectionId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
