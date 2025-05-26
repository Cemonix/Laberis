using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs;
using server.Services.Interfaces;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProjects(
            [FromQuery] string? filterOn = null,
            [FromQuery] string? filterQuery = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isAscending = true,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25 // Default pageSize matches IGenericRepository
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
    }
}
