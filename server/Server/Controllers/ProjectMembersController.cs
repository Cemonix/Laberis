using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Exceptions;
using server.Models.DTOs.ProjectInvitation;
using server.Models.DTOs.ProjectMember;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("project")]
public class ProjectMembersController : ControllerBase
{
    private readonly IProjectMemberService _projectMemberService;
    private readonly IProjectInvitationService _projectInvitationService;
    private readonly ILogger<ProjectMembersController> _logger;

    public ProjectMembersController(
        IProjectMemberService projectMemberService, 
        IProjectInvitationService projectInvitationService,
        ILogger<ProjectMembersController> logger)
    {
        _projectMemberService = projectMemberService ?? throw new ArgumentNullException(nameof(projectMemberService));
        _projectInvitationService = projectInvitationService ?? throw new ArgumentNullException(nameof(projectInvitationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all project members for a specific project with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="projectId">The ID of the project to get members for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "role", "user_id").</param>
    /// <param name="filterQuery">The value to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "joined_at", "role", "user_id").</param>
    /// <param name="isAscending">Whether to sort in ascending order (true) or descending (false).</param>
    /// <param name="pageNumber">The page number for pagination (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A list of project member DTOs.</returns>
    /// <response code="200">Returns the list of project member DTOs.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [EnableRateLimiting("public")]
    [ProducesResponseType(typeof(IEnumerable<ProjectMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProjectMembersForProject(
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
            var projectMembers = await _projectMemberService.GetProjectMembersAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(projectMembers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching project members for project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets a specific project member by project and user ID.
    /// </summary>
    /// <param name="projectId">The ID of the project the member belongs to.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The requested project member.</returns>
    [HttpGet("by-user/{userId}")]
    [EnableRateLimiting("public")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectMemberByUserId(int projectId, string userId)
    {
        try
        {
            var projectMember = await _projectMemberService.GetProjectMemberAsync(projectId, userId);

            if (projectMember == null)
            {
                return NotFound($"Project member for user {userId} in project {projectId} not found.");
            }

            return Ok(projectMember);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching project member for user {UserId} in project {ProjectId}.", userId, projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Gets all projects where the current user is a member.
    /// </summary>
    /// <returns>A list of project member DTOs for the current user.</returns>
    [HttpGet("~/api/project-members/my-memberships")]
    [EnableRateLimiting("public")]
    [ProducesResponseType(typeof(IEnumerable<ProjectMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProjectMemberships()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var projectMemberships = await _projectMemberService.GetUserProjectMembershipsAsync(userId.ToString());
            return Ok(projectMemberships);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching project memberships for current user.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Invites a user to a project by email (creates invitation for new users or adds existing users).
    /// </summary>
    /// <param name="projectId">The ID of the project to invite the member to.</param>
    /// <param name="createProjectInvitationDto">The project invitation data.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="400">If the invitation data is invalid.</response>
    /// <response code="409">If user is already a member or invitation already exists.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost("invite")]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteProjectMember(int projectId, [FromBody] CreateProjectInvitationDto createProjectInvitationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var inviterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(inviterUserId))
            {
                return Unauthorized("User ID not found in token");
            }

            await _projectInvitationService.InviteUserByEmailAsync(projectId, createProjectInvitationDto, inviterUserId);
            
            return Ok(new { message = "Invitation sent successfully." });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while inviting project member to project {ProjectId}.", projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Updates an existing project member's role or other details.
    /// </summary>
    /// <param name="projectId">The ID of the project the member belongs to.</param>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="updateProjectMemberDto">The project member update data.</param>
    /// <returns>The updated project member.</returns>
    /// <response code="200">Returns the updated project member.</response>
    /// <response code="400">If the project member data is invalid.</response>
    /// <response code="404">If the project member is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPut("by-user/{userId}")]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProjectMember(int projectId, string userId, [FromBody] UpdateProjectMemberDto updateProjectMemberDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedProjectMember = await _projectMemberService.UpdateProjectMemberAsync(projectId, userId, updateProjectMemberDto);

            if (updatedProjectMember == null)
            {
                return NotFound($"Project member for user {userId} in project {projectId} not found.");
            }

            return Ok(updatedProjectMember);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating project member for user {UserId} in project {ProjectId}.", userId, projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Removes a member from a project.
    /// </summary>
    /// <param name="projectId">The ID of the project the member belongs to.</param>
    /// <param name="userId">The ID of the user to remove.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="404">If the project member is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpDelete("by-user/{userId}")]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveProjectMember(int projectId, string userId)
    {
        try
        {
            var result = await _projectMemberService.RemoveProjectMemberAsync(projectId, userId);

            if (!result)
            {
                return NotFound($"Project member for user {userId} in project {projectId} not found.");
            }

            return Ok(new { message = "Project member removed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing project member for user {UserId} from project {ProjectId}.", userId, projectId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    /// <summary>
    /// Updates an existing project member's role by email.
    /// </summary>
    /// <param name="projectId">The ID of the project the member belongs to.</param>
    /// <param name="request">The request containing the email and new role.</param>
    /// <returns>The updated project member.</returns>
    /// <response code="200">Returns the updated project member.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the project member is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPut("update-by-email")]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProjectMemberByEmail(int projectId, [FromBody] UpdateProjectMemberByEmailDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updateDto = new UpdateProjectMemberDto { Role = request.Role };
            var updatedProjectMember = await _projectMemberService.UpdateProjectMemberByEmailAsync(projectId, request.Email, updateDto);

            if (updatedProjectMember == null)
            {
                return NotFound($"Project member with email {request.Email} not found in project {projectId}.");
            }

            return Ok(updatedProjectMember);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project member by email {Email} in project {ProjectId}.", request.Email, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
        }
    }

    /// <summary>
    /// Removes a member from a project by email.
    /// </summary>
    /// <param name="projectId">The ID of the project the member belongs to.</param>
    /// <param name="request">The request containing the email of the user to remove.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Returns a success message.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="404">If the project member is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpDelete("remove-by-email")]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveProjectMemberByEmail(int projectId, [FromBody] RemoveProjectMemberByEmailDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _projectMemberService.RemoveProjectMemberByEmailAsync(projectId, request.Email);

            if (!result)
            {
                return NotFound($"Project member with email {request.Email} not found in project {projectId}.");
            }

            return Ok(new { message = "Project member removed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing project member by email {Email} from project {ProjectId}.", request.Email, projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
        }
    }
}
