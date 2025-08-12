using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.Configuration;
using server.Services.Interfaces;
using System.Security.Claims;

namespace server.Controllers
{
    /// <summary>
    /// Controller for serving application configuration to the frontend.
    /// Provides permission rules, feature flags, and other configuration data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAuthenticatedUser")]
    [EnableRateLimiting("public")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IPermissionConfigurationService _permissionConfigurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IPermissionConfigurationService permissionConfigurationService,
            ILogger<ConfigurationController> logger)
        {
            _permissionConfigurationService = permissionConfigurationService ?? throw new ArgumentNullException(nameof(permissionConfigurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Reloads the permission configuration from the configuration file.
        /// This endpoint is restricted to admin users only.
        /// </summary>
        /// <returns>Success message</returns>
        /// <response code="200">Configuration reloaded successfully</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an admin</response>
        /// <response code="500">If an error occurs reloading the configuration</response>
        [HttpPost("permissions/reload")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ReloadPermissionConfiguration()
        {
            try
            {
                _logger.LogInformation("Admin user requesting permission configuration reload");
                
                _permissionConfigurationService.ReloadConfiguration();
                
                _logger.LogInformation("Permission configuration reloaded successfully");
                return Ok(new { message = "Permission configuration reloaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload permission configuration");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while reloading the permission configuration");
            }
        }

        /// <summary>
        /// Gets page-specific permissions for the current user.
        /// Hybrid mode endpoint that provides permissions on-demand for specific pages/routes.
        /// </summary>
        /// <param name="page">The page/route identifier to get permissions for</param>
        /// <param name="projectId">Optional project ID for project-specific permissions</param>
        /// <returns>User permissions for the requested page</returns>
        /// <response code="200">Returns the page permissions</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If an error occurs loading the permissions</response>
        [HttpGet("permissions/page")]
        [ProducesResponseType(typeof(HashSet<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPagePermissions([FromQuery] string page, [FromQuery] int? projectId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(page))
                {
                    return BadRequest("Page parameter is required");
                }

                _logger.LogDebug("Fetching page permissions for page '{Page}' with project {ProjectId}", page, projectId);

                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var permissions = await _permissionConfigurationService.GetPagePermissionsAsync(userId, page, projectId);
                
                _logger.LogDebug("Successfully retrieved {PermissionCount} permissions for page '{Page}' and project {ProjectId}", 
                    permissions.Count, page, projectId);

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve page permissions for page '{Page}' and project {ProjectId}", page, projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while loading the page permissions");
            }
        }

        /// <summary>
        /// Gets the full user permission context including all project memberships and global permissions.
        /// This can be used for comprehensive permission caching on the frontend.
        /// </summary>
        /// <returns>Complete user permission context</returns>
        /// <response code="200">Returns the user permission context</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If an error occurs loading the permissions</response>
        [HttpGet("permissions/user-context")]
        [ProducesResponseType(typeof(UserPermissionContext), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserPermissionContext()
        {
            try
            {
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                _logger.LogDebug("Building permission context for user {UserId}", userId);

                var context = await _permissionConfigurationService.BuildUserPermissionContextAsync(userId);
                
                _logger.LogDebug("Successfully built permission context for user {UserId} with {PermissionCount} total permissions across {ProjectCount} projects", 
                    userId, context.Permissions.Count, context.ProjectPermissions.Count);

                return Ok(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build permission context for user");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while building the permission context");
            }
        }
    }
}