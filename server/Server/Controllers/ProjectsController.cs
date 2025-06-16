using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.Common;
using server.Models.DTOs.Project;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all projects with optional filtering, sorting, and pagination.
        /// </summary>
        /// <param name="filterOn">The field to filter on (e.g., "name", "project_type", "status").</param>
        /// <param name="filterQuery">The value to filter by.</param>
        /// <param name="sortBy">The field to sort by (e.g., "name", "created_at").</param>
        /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
        /// <param name="pageNumber">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A list of project DTOs.</returns>
        /// <response code="200">Returns the list of project DTOs.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProjects(
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
                var projects = await _projectService.GetAllProjectsAsync(
                    filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all projects.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred. Please try again later."
                );
            }
        }

        /// <summary>
        /// Gets a specific project by its unique ID.
        /// </summary>
        /// <param name="id">The ID of the project.</param>
        /// <returns>The requested project.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectById(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);

            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            return Ok(project);
        }

        /// <summary>
        /// Creates a new project.
        /// </summary>
        /// <remarks>
        /// Creates a new project along with a default data source (Minio bucket). The user making the request will be set as the project owner.
        /// </remarks>
        /// <param name="createProjectDto">The project creation data.</param>
        /// <returns>The newly created project.</returns>
        /// <response code="201">Returns the newly created project and its location.</response>
        /// <response code="400">If the request data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto createProjectDto)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(ownerId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            try
            {
                var newProject = await _projectService.CreateProjectAsync(createProjectDto, ownerId);
                return CreatedAtAction(nameof(GetAllProjects), new { id = newProject.Id }, newProject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating project '{ProjectName}'.", createProjectDto.Name);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        /// <param name="id">The ID of the project to update.</param>
        /// <param name="updateDto">The data to update the project with.</param>
        /// <returns>The updated project.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto updateDto)
        {
            var updatedProject = await _projectService.UpdateProjectAsync(id, updateDto);

            if (updatedProject == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            return Ok(updatedProject);
        }

        /// <summary>
        /// Soft-deletes a project by its ID.
        /// </summary>
        /// <remarks>
        /// This endpoint marks the project as pending deletion instead of permanently deleting it.
        /// This allows for potential recovery or auditing of deleted projects in the future.
        /// </remarks>
        /// <param name="id">The ID of the project to delete.</param>
        /// <returns>A confirmation of deletion.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var success = await _projectService.DeleteProjectAsync(id);

            if (!success)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            return NoContent();
        }
    }
}