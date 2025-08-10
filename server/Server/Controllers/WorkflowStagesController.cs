using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.WorkflowStage;
using server.Services.Interfaces;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/workflows/{workflowId:int}/stages")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class WorkflowStagesController : ControllerBase
{
    private readonly IWorkflowStageService _workflowStageService;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowStageAssignmentService _assignmentService;
    private readonly ILogger<WorkflowStagesController> _logger;

    public WorkflowStagesController(
        IWorkflowStageService workflowStageService,
        IWorkflowService workflowService,
        IWorkflowStageAssignmentService assignmentService,
        ILogger<WorkflowStagesController> logger)
    {
        _workflowStageService = workflowStageService ?? throw new ArgumentNullException(nameof(workflowStageService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _assignmentService = assignmentService ?? throw new ArgumentNullException(nameof(assignmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all workflow stages for a specific workflow with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow to get stages for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "name", "is_final").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "name", "order_index", "created_at").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of workflow stage DTOs.</returns>
    /// <response code="200">Returns the list of workflow stage DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowStageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowStagesForWorkflow(
        int projectId,
        int workflowId,
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
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // For pipeline visualization, we typically want all stages with connections
            // But we should respect pagination parameters for API consistency
            var workflowStages = await _workflowStageService.GetWorkflowStagesAsync(
                workflowId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize
            );
            return Ok(workflowStages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching workflow stages for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific workflow stage by its unique ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow the stage belongs to.</param>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <returns>The requested workflow stage.</returns>
    [HttpGet("{stageId:int}")]
    [ProducesResponseType(typeof(WorkflowStageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowStageById(int projectId, int workflowId, int stageId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            var workflowStage = await _workflowStageService.GetWorkflowStageByIdAsync(stageId);

            if (workflowStage == null)
            {
                return NotFound($"Workflow stage with ID {stageId} not found.");
            }

            // Validate that the stage belongs to the specified workflow
            if (workflowStage.WorkflowId != workflowId)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            return Ok(workflowStage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching workflow stage {StageId}.", stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Creates a new workflow stage for a workflow.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow to create the stage for.</param>
    /// <param name="createWorkflowStageDto">The workflow stage creation data.</param>
    /// <returns>The created workflow stage.</returns>
    /// <response code="201">Returns the newly created workflow stage.</response>
    /// <response code="400">If the workflow stage data is invalid.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowStageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWorkflowStage(int projectId, int workflowId, [FromBody] CreateWorkflowStageDto createWorkflowStageDto)
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

            var createdWorkflowStage = await _workflowStageService.CreateWorkflowStageAsync(workflowId, createWorkflowStageDto);
            
            return CreatedAtAction(
                nameof(GetWorkflowStageById),
                new { projectId, workflowId, stageId = createdWorkflowStage.Id },
                createdWorkflowStage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating workflow stage for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Updates an existing workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="stageId">The ID of the workflow stage to update.</param>
    /// <param name="updateWorkflowStageDto">The workflow stage update data.</param>
    /// <returns>The updated workflow stage.</returns>
    /// <response code="200">Returns the updated workflow stage.</response>
    /// <response code="400">If the workflow stage data is invalid.</response>
    /// <response code="404">If the workflow stage is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPut("{stageId:int}")]
    [ProducesResponseType(typeof(WorkflowStageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateWorkflowStage(
        int projectId, int workflowId, int stageId, [FromBody] UpdateWorkflowStageDto updateWorkflowStageDto
    )
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

            // Validate that the stage belongs to the specified workflow
            var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
            if (!isValidStage)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            var updatedWorkflowStage = await _workflowStageService.UpdateWorkflowStageAsync(stageId, updateWorkflowStageDto);

            if (updatedWorkflowStage == null)
            {
                return NotFound($"Workflow stage with ID {stageId} not found.");
            }

            return Ok(updatedWorkflowStage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating workflow stage {StageId}.", stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Deletes a workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="stageId">The ID of the workflow stage to delete.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="404">If the workflow stage is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpDelete("{stageId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteWorkflowStage(int projectId, int workflowId, int stageId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the stage belongs to the specified workflow
            var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
            if (!isValidStage)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            var result = await _workflowStageService.DeleteWorkflowStageAsync(stageId);

            if (!result)
            {
                return NotFound($"Workflow stage with ID {stageId} not found.");
            }

            return Ok(new { message = "Workflow stage deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting workflow stage {StageId}.", stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Reorders workflow stages within a workflow.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow to reorder stages for.</param>
    /// <param name="stageIds">The list of stage IDs in the new order.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="400">If the stage ID list is invalid.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderWorkflowStages(int projectId, int workflowId, [FromBody] List<int> stageIds)
    {
        try
        {
            if (stageIds == null || stageIds.Count == 0)
            {
                return BadRequest("Stage IDs list cannot be empty.");
            }

            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that all stages belong to the specified workflow
            foreach (var stageId in stageIds)
            {
                var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
                if (!isValidStage)
                {
                    return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
                }
            }

            // Convert List<int> to Dictionary<int, int> where key is stageId and value is new order
            var stageOrderMap = stageIds.Select((stageId, index) => new { stageId, order = index + 1 })
                .ToDictionary(x => x.stageId, x => x.order);

            await _workflowStageService.ReorderWorkflowStagesAsync(workflowId, stageOrderMap);

            return Ok(new { message = "Workflow stages reordered successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reordering workflow stages for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all workflow stages with connections for pipeline visualization.
    /// This endpoint returns all stages without pagination for building the workflow graph.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow to get stages for.</param>
    /// <returns>A list of workflow stages with connection information.</returns>
    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(IEnumerable<WorkflowStageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowStagesForPipeline(int projectId, int workflowId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            var workflowStages = await _workflowStageService.GetWorkflowStagesWithConnectionsAsync(workflowId);
            return Ok(workflowStages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching pipeline stages for workflow {WorkflowId}.", workflowId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all assignments for a specific workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <returns>A list of workflow stage assignments.</returns>
    [HttpGet("{stageId:int}/assignments")]
    [ProducesResponseType(typeof(IEnumerable<WorkflowStageAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStageAssignments(int projectId, int workflowId, int stageId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the stage belongs to the workflow
            var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
            if (!isValidStage)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            var assignments = await _assignmentService.GetAssignmentsForStageAsync(stageId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching assignments for stage {StageId}.", stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Assigns a project member to a workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <param name="createDto">The assignment creation data.</param>
    /// <returns>The created assignment.</returns>
    [HttpPost("{stageId:int}/assignments")]
    [ProducesResponseType(typeof(WorkflowStageAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateStageAssignment(
        int projectId, 
        int workflowId, 
        int stageId,
        [FromBody] CreateWorkflowStageAssignmentDto createDto)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the stage belongs to the workflow
            var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
            if (!isValidStage)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            // Ensure the DTO has the correct stage ID
            if (createDto.WorkflowStageId != stageId)
            {
                return BadRequest($"Stage ID in URL ({stageId}) does not match stage ID in request body ({createDto.WorkflowStageId}).");
            }

            var assignment = await _assignmentService.CreateAssignmentAsync(createDto);
            return CreatedAtAction(
                nameof(GetStageAssignments),
                new { projectId, workflowId, stageId },
                assignment);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid assignment request for stage {StageId}.", stageId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating assignment for stage {StageId}.", stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Removes an assignment from a workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <param name="assignmentId">The ID of the assignment to remove.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{stageId:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStageAssignment(
        int projectId, 
        int workflowId, 
        int stageId, 
        int assignmentId)
    {
        try
        {
            // Validate that the workflow belongs to the project
            var isValidWorkflow = await _workflowService.ValidateWorkflowBelongsToProjectAsync(workflowId, projectId);
            if (!isValidWorkflow)
            {
                return BadRequest($"Workflow {workflowId} does not belong to project {projectId}.");
            }

            // Validate that the stage belongs to the workflow
            var isValidStage = await _workflowStageService.ValidateStageBelongsToWorkflowAsync(stageId, workflowId);
            if (!isValidStage)
            {
                return BadRequest($"Stage {stageId} does not belong to workflow {workflowId}.");
            }

            var deleted = await _assignmentService.DeleteAssignmentAsync(assignmentId);
            if (!deleted)
            {
                return NotFound($"Assignment {assignmentId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting assignment {AssignmentId} from stage {StageId}.", assignmentId, stageId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
