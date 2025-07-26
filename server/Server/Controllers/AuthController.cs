using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using server.Models.DTOs.Auth;
using server.Services.Interfaces;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authManager, ILogger<AuthController> logger)
    {
        _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="registerDto">Registration data</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);
            
            var response = await _authManager.RegisterAsync(registerDto);
            
            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
            throw;
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);
            
            var response = await _authManager.LoginAsync(loginDto);
            
            _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);
            throw;
        }
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>A new set of authentication tokens.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var response = await _authManager.RefreshTokenAsync(request.Token);
            return Ok(response);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed.");
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logout the current user and revoke their refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _authManager.RevokeRefreshTokenAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user data</returns>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserDto> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email))
        {
            return Unauthorized("Invalid token claims");
        }

        var userDto = new UserDto
        {
            UserName = userName,
            Email = email,
            Roles = [.. User.FindAll(ClaimTypes.Role).Select(role => role.Value)]
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Change the current user's password
    /// </summary>
    /// <param name="changePasswordDto">Password change data</param>
    /// <returns>Success message</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("Password change attempt for user: {UserId}", userId);
            
            await _authManager.ChangePasswordAsync(userId, changePasswordDto);
            
            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user");
            throw;
        }
    }
}
