using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using server.Models.DTOs.Auth;
using server.Services;
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
            
            // Set refresh token as httpOnly cookie
            SetRefreshTokenCookie(AuthService.GenerateRefreshToken());

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
            
            // Set refresh token as httpOnly cookie
            SetRefreshTokenCookie(AuthService.GenerateRefreshToken());

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
    /// <returns>A new set of authentication tokens.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken()
    {
        try
        {
            // Get refresh token from httpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token not found in cookies.");
                return Unauthorized(new { message = "Refresh token not found" });
            }

            var response = await _authManager.RefreshTokenAsync(refreshToken);
            
            SetRefreshTokenCookie(AuthService.GenerateRefreshToken()); // Set new refresh token cookie
            
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
        
        // Clear refresh token cookie
        ClearRefreshTokenCookie();
        
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

    /// <summary>
    /// Send email verification to the current user
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("send-email-verification")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendEmailVerification()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("Email verification request for user: {UserId}", userId);
            
            await _authManager.SendEmailVerificationAsync(userId);
            
            _logger.LogInformation("Email verification sent successfully for user: {UserId}", userId);
            return Ok(new { message = "Email verification sent. Please check your email." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification");
            throw;
        }
    }

    /// <summary>
    /// Verify email address using verification token
    /// </summary>
    /// <param name="token">The email verification token</param>
    /// <returns>Success message</returns>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            _logger.LogInformation("Email verification attempt");
            
            var success = await _authManager.VerifyEmailAsync(token);
            
            if (success)
            {
                _logger.LogInformation("Email verified successfully");
                return Ok(new { message = "Email verified successfully. You can now log in." });
            }
            
            return BadRequest(new { message = "Email verification failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed");
            throw;
        }
    }

    /// <summary>
    /// Resend email verification to a user (unauthenticated endpoint)
    /// </summary>
    /// <param name="request">Request containing the email address</param>
    /// <returns>Success message</returns>
    [HttpPost("resend-email-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendEmailVerification([FromBody] ResendEmailVerificationDto request)
    {
        try
        {
            _logger.LogInformation("Resend email verification request for email: {Email}", request.Email);
            
            await _authManager.ResendEmailVerificationAsync(request.Email);
            
            _logger.LogInformation("Email verification resent successfully for email: {Email}", request.Email);
            return Ok(new { message = "Verification email has been resent. Please check your email." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend email verification for email: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Set refresh token as secure httpOnly cookie
    /// </summary>
    /// <param name="refreshToken">The refresh token to set</param>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Only send over HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7), // Match refresh token expiration
            Path = "/",
            IsEssential = true
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Clear refresh token cookie
    /// </summary>
    private void ClearRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1), // Expire immediately
            Path = "/",
            IsEssential = true
        };

        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }
}
