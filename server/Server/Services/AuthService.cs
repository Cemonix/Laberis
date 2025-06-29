using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using server.Configs;
using server.Exceptions;
using server.Models.Domain.Enums;
using server.Models.DTOs.Auth;
using server.Services.Interfaces;

namespace server.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IProjectInvitationService _projectInvitationService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IProjectInvitationService projectInvitationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _jwtSettings = (jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectInvitationService = projectInvitationService ?? throw new ArgumentNullException(nameof(projectInvitationService));
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        _logger.LogInformation("Attempting to register user with email: {Email}", registerDto.Email);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: User with email {Email} already exists", registerDto.Email);
            throw new ConflictException("User with this email already exists");
        }

        // Check if username is taken
        var existingUsername = await _userManager.FindByNameAsync(registerDto.UserName);
        if (existingUsername != null)
        {
            _logger.LogWarning("Registration failed: Username {UserName} is already taken", registerDto.UserName);
            throw new ConflictException("Username is already taken");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            EmailConfirmed = true, // TODO: For now, we'll auto-confirm emails
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Registration failed for {Email}: {Errors}", registerDto.Email, errors);
            throw new DatabaseException($"Registration failed: {errors}");
        }

        // If user creation is successful, add them to the "User" role.
        _logger.LogInformation("Assigning default {Role} role to {Email}", user.Email, Role.USER);
        var roleResult = await _userManager.AddToRoleAsync(user, Role.USER.ToString());

        if (!roleResult.Succeeded)
        {
            var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogCritical(
                "Could not assign default {Role} role to {Email}: {Errors}. Rolling back user creation.",
                Role.USER,
                user.Email,
                roleErrors
            );

            await _userManager.DeleteAsync(user);
            throw new DatabaseException($"Could not assign default {Role.USER} role: {roleErrors}");
        }

        _logger.LogInformation("User {Email} registered successfully with ID: {UserId}", user.Email, user.Id);

        // Process invitation token if provided
        if (!string.IsNullOrEmpty(registerDto.InviteToken))
        {
            _logger.LogInformation("Processing invitation token for user {Email}", user.Email);
            var tokenProcessed = await _projectInvitationService.ProcessInvitationTokenAsync(registerDto.InviteToken, user.Id);
            
            if (!tokenProcessed)
            {
                _logger.LogWarning("Failed to process invitation token {Token} for user {Email}", registerDto.InviteToken, user.Email);
                // Note: We don't throw an exception here as the user is already created
                // The invitation failure should be handled gracefully
            }
            else
            {
                _logger.LogInformation("Successfully processed invitation token for user {Email}", user.Email);
            }
        }

        // Generate token`
        var accesstoken = await GenerateTokenAsync(user);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiration);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = accesstoken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!
            }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("Attempting to login user with email: {Email}", loginDto.Email);

        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found", loginDto.Email);
            throw new ValidationException("Invalid email or password");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}", loginDto.Email);
            throw new ValidationException("Invalid email or password");
        }

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        // Generate Access Token
        var accessToken = await GenerateTokenAsync(user);

        // Generate and Save Refresh Token
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiration);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Roles = await _userManager.GetRolesAsync(user)
            }
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token)
    {
        var user = _userManager.Users.SingleOrDefault(u => u.RefreshToken == token);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired refresh token attempt.");
            throw new SecurityTokenException("Invalid refresh token");
        }

        // Generate new tokens
        var newAccessToken = await GenerateTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        // Update user with the new refresh token (Token Rotation)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiration);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = await _userManager.GetRolesAsync(user)
            }
        };
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles as claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.ValidIssuer,
            audience: _jwtSettings.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task RevokeRefreshTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Logout failed: User with ID {UserId} not found for token revocation.", userId);
            return;
        }

        // Nullify the refresh token and its expiry
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Refresh token for user {UserId} has been revoked.", userId);
    }
    
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[128];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
