using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.ProjectInvitation;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers;

[Route("api/projects/{projectId:int}/invitations")]
[ApiController]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IProjectInvitationService _invitationService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(
        IProjectInvitationService invitationService,
        ILogger<InvitationsController> logger)
    {
        _invitationService = invitationService ?? throw new ArgumentNullException(nameof(invitationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invites a user to a project by email
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="createDto">The invitation data</param>
    /// <returns>Success response</returns>
    /// <response code="200">Invitation sent successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Project or inviter not found</response>
    /// <response code="409">User is already a member or invitation already exists</response>
    [HttpPost]
    [Authorize(Policy = "CanManageProjectMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteUserByEmail(int projectId, [FromBody] CreateProjectInvitationDto createDto)
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

            await _invitationService.InviteUserByEmailAsync(projectId, createDto, inviterUserId);

            return Ok(new { message = "Invitation sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation for project {ProjectId} to email {Email}", projectId, createDto.Email);
            throw;
        }
    }

    /// <summary>
    /// Gets invitation details by token (for registration validation)
    /// </summary>
    /// <param name="token">The invitation token</param>
    /// <returns>Invitation details if valid</returns>
    [HttpGet("~/api/invitations/validate/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProjectInvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateInvitationToken(string token)
    {
        try
        {
            var invitation = await _invitationService.GetValidInvitationByTokenAsync(token);
            
            if (invitation == null)
            {
                return NotFound("Invalid or expired invitation token");
            }

            return Ok(invitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token {Token}", token);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while validating the invitation");
        }
    }

    /// <summary>
    /// Accept an invitation with a token (for existing users)
    /// </summary>
    /// <param name="token">The invitation token</param>
    /// <returns>Success response</returns>
    [HttpPost("~/api/invitations/accept/{token}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User information not found in token");
            }

            var success = await _invitationService.ProcessInvitationTokenAsync(token, userId, userEmail);
            
            if (!success)
            {
                return BadRequest("Invalid, expired, or already accepted invitation token");
            }

            return Ok(new { message = "Invitation accepted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation with token {Token}", token);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while accepting the invitation");
        }
    }
}
