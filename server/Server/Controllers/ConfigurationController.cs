using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Models.DTOs.Configuration;
using server.Services.Interfaces;

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
        /// Gets the project permission configuration for the frontend.
        /// Returns permission definitions and role-based permission mappings.
        /// </summary>
        /// <returns>Permission configuration</returns>
        /// <response code="200">Returns the permission configuration</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If an error occurs loading the configuration</response>
        [HttpGet("permissions")]
        [ProducesResponseType(typeof(PermissionConfigurationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPermissionConfiguration()
        {
            try
            {
                _logger.LogDebug("Fetching permission configuration for user");
                
                var configuration = _permissionConfigurationService.GetPermissionConfiguration();
                
                _logger.LogDebug("Successfully retrieved permission configuration with {PermissionCount} permission categories and {RoleCount} roles",
                    configuration.Permissions.Count,
                    configuration.RolePermissions.Count);

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve permission configuration");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while loading the permission configuration");
            }
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
    }
}