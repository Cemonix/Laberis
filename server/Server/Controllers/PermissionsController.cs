using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using server.Models.DTOs.Authz;
using server.Services.Interfaces;

namespace server.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ILogger<PermissionsController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionConfigurationService _permissionConfig;
    private readonly IProjectMembershipService _membershipService;

    public PermissionsController(
        ILogger<PermissionsController> logger,
        UserManager<ApplicationUser> userManager,
        IPermissionConfigurationService permissionConfig,
        IProjectMembershipService membershipService)
    {
        _logger = logger;
        _userManager = userManager;
        _permissionConfig = permissionConfig;
        _membershipService = membershipService;
    }

    /// <summary>
    /// Reloads the permission configuration from the configuration source.
    /// Restricted to administrators.
    /// </summary>
    /// <returns>Success message on completion.</returns>
    /// <response code="200">Configuration reloaded successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not an admin</response>
    /// <response code="500">If an error occurs during reload</response>
    [HttpPost("reload")]
    [Authorize(Policy = "RequireAdminRole")]
    public IActionResult ReloadPermissionConfiguration()
    {
        try
        {
            _logger.LogInformation("Admin requested permission configuration reload");
            _permissionConfig.ReloadConfiguration();
            _logger.LogInformation("Permission configuration reload completed successfully");
            return Ok(new { message = "Permission configuration reloaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload permission configuration");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while reloading the permission configuration");
        }
    }

    [HttpPost("authorize")]
    public async Task<ActionResult<AuthorizationResponseDto>> AuthorizeAsync([FromBody] AuthorizationCheckDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Permission))
        {
            return BadRequest("Permission is required");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var allow = await EvaluateAsync(user, request.Permission, request.Context);
        return Ok(new AuthorizationResponseDto { Allow = allow, PolicyVersion = null });
    }

    [HttpPost("authorize/batch")]
    public async Task<ActionResult<AuthorizationBatchResponseDto>> AuthorizeBatchAsync([FromBody] AuthorizationBatchRequestDto request)
    {
        if (request is null || request.Checks is null)
        {
            return BadRequest("Checks are required");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var results = new List<bool>(request.Checks.Count);
        foreach (var check in request.Checks)
        {
            if (check is null || string.IsNullOrWhiteSpace(check.Permission))
            {
                results.Add(false);
                continue;
            }
            var allow = await EvaluateAsync(user, check.Permission, check.Context);
            results.Add(allow);
        }

        return Ok(new AuthorizationBatchResponseDto { Results = results, PolicyVersion = null });
    }

    private async Task<bool> EvaluateAsync(ApplicationUser user, string permission, AuthorizationContextDto? context)
    {
        // Global permissions (no project context)
        if (context?.ProjectId is null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var perms = _permissionConfig.GetGlobalPermissionsForUser(roles);
            var allow = perms.Contains(permission);
            _logger.LogDebug("Authorize (global): user {UserId}, perm {Perm} => {Allow}", user.Id, permission, allow);
            return allow;
        }

        // Project-scoped permissions
        var projectId = context.ProjectId.Value;

        // First, confirm membership
        var isMember = await _membershipService.IsProjectMemberAsync(user.Id, projectId);
        if (!isMember)
        {
            _logger.LogDebug("Authorize: user {UserId} not a member of project {ProjectId}", user.Id, projectId);
            return false;
        }

        var role = await _membershipService.GetUserRoleInProjectAsync(user.Id, projectId);
        if (role is null)
        {
            _logger.LogDebug("Authorize: user {UserId} has no role in project {ProjectId}", user.Id, projectId);
            return false;
        }

        var allowRole = _permissionConfig.HasPermission(role.Value, permission);
        _logger.LogDebug("Authorize: user {UserId} role {Role} in project {ProjectId}, perm {Perm} => {Allow}", 
            user.Id, role.Value, projectId, permission, allowRole);
        return allowRole;
    }
}
