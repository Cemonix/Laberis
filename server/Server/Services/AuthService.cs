using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using server.Configs;
using server.Exceptions;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.Auth;
using server.Services.Interfaces;
using server.Data;
using Microsoft.EntityFrameworkCore;
using server.Models.DTOs.Configuration;

namespace server.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly WebAppSettings _webAppSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IProjectInvitationService _projectInvitationService;
    private readonly IEmailService _emailService;
    private readonly LaberisDbContext _context;
    private readonly IPermissionConfigurationService _permissionConfigurationService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        IOptions<WebAppSettings> webAppSettings,
        ILogger<AuthService> logger,
        IProjectInvitationService projectInvitationService,
        IEmailService emailService,
        LaberisDbContext context,
        IPermissionConfigurationService permissionConfigurationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _jwtSettings = (jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings))).Value;
        _webAppSettings = (webAppSettings ?? throw new ArgumentNullException(nameof(webAppSettings))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectInvitationService = projectInvitationService ?? throw new ArgumentNullException(nameof(projectInvitationService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionConfigurationService = permissionConfigurationService ?? throw new ArgumentNullException(nameof(permissionConfigurationService));
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
            EmailConfirmed = false, // Email verification required
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

        // Send email verification
        try
        {
            await SendEmailVerificationAsync(user.Id);
            _logger.LogInformation("Email verification sent to {Email} during registration", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification during registration for {Email}", user.Email);
            // Continue with registration - user can request verification later
        }

        // Process invitation token if provided
        if (!string.IsNullOrEmpty(registerDto.InviteToken))
        {
            _logger.LogInformation("Processing invitation token for user {Email}", user.Email);
            var tokenProcessed = await _projectInvitationService.ProcessInvitationTokenAsync(registerDto.InviteToken, user.Id, user.Email);
            
            if (!tokenProcessed)
            {
                _logger.LogWarning("Failed to process invitation token {Token} for user {Email}", registerDto.InviteToken, user.Email);
                // Note: We don't throw an exception here as the user is already created
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            RefreshToken = refreshToken, // Include refresh token for cookie setting
            User = await BuildUserDtoAsync(user)
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

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {Email}", loginDto.Email);
            throw new ValidationException("Please verify your email address before logging in. Check your email for a verification link, or request a new one.");
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            RefreshToken = refreshToken, // Include refresh token for cookie setting
            User = await BuildUserDtoAsync(user)
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            RefreshToken = newRefreshToken, // Include refresh token for cookie setting
            User = await BuildUserDtoAsync(user)
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

    /// <summary>
    /// Builds a UserDto with permission context for the specified user.
    /// Includes project memberships and role-based permissions.
    /// </summary>
    /// <param name="user">The user to build DTO for</param>
    /// <returns>UserDto with complete permission context</returns>
    private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        
        UserPermissionContext? permissionContext = null;
        try
        {
            permissionContext = await _permissionConfigurationService.BuildUserPermissionContextAsync(user.Id);
            _logger.LogDebug("Built permission context for user {UserId} with {PermissionCount} total permissions", 
                user.Id, permissionContext.Permissions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build permission context for user {UserId}. User will have limited permissions.", user.Id);
            // Continue without permission context rather than failing the entire login
        }

        return new UserDto
        {
            UserName = user.UserName!,
            Email = user.Email!,
            Roles = roles,
            PermissionContext = permissionContext
        };
    }

    public async System.Threading.Tasks.Task RevokeRefreshTokenAsync(string userId)
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

    public async System.Threading.Tasks.Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        _logger.LogInformation("Attempting to change password for user: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Password change failed: User with ID {UserId} not found", userId);
            throw new NotFoundException("User not found");
        }

        // Verify current password
        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword);
        if (!isCurrentPasswordValid)
        {
            _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
            throw new ValidationException("Current password is incorrect");
        }

        // Check if new password is different from current password
        var isSamePassword = await _userManager.CheckPasswordAsync(user, changePasswordDto.NewPassword);
        if (isSamePassword)
        {
            _logger.LogWarning("Password change failed: New password is the same as current password for user {UserId}", userId);
            throw new ValidationException("New password must be different from current password");
        }

        // Change password
        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Password change failed for user {UserId}: {Errors}", userId, errors);
            throw new ValidationException($"Password change failed: {errors}");
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
    }

    public async System.Threading.Tasks.Task SendEmailVerificationAsync(string userId)
    {
        _logger.LogInformation("Attempting to send email verification for user: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Send email verification failed: User with ID {UserId} not found", userId);
            throw new NotFoundException("User not found");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogWarning("Send email verification failed: User {UserId} already has confirmed email", userId);
            throw new ValidationException("Email is already verified");
        }

        // Create and send email verification token
        await CreateAndSendEmailVerificationTokenAsync(user);

        _logger.LogInformation("Email verification sent successfully for user: {UserId}", userId);
    }

    public async System.Threading.Tasks.Task<bool> VerifyEmailAsync(string token)
    {
        _logger.LogInformation("Attempting to verify email with token");

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Email verification failed: Token is null or empty");
            throw new ValidationException("Verification token is required");
        }

        var emailVerificationToken = await _context.EmailVerificationTokens
            .IgnoreQueryFilters() // Ignore query filter to find any token
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (emailVerificationToken == null)
        {
            _logger.LogWarning("Email verification failed: Invalid token");
            throw new ValidationException("Invalid verification token");
        }

        if (emailVerificationToken.IsUsed)
        {
            _logger.LogWarning("Email verification failed: Token already used for user {UserId}", emailVerificationToken.UserId);
            throw new ValidationException("This verification link has already been used");
        }

        if (emailVerificationToken.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Email verification failed: Token expired for user {UserId}", emailVerificationToken.UserId);
            throw new ValidationException("This verification link has expired. Please request a new one.");
        }

        var user = emailVerificationToken.User;
        if (user.EmailConfirmed)
        {
            _logger.LogWarning("Email verification failed: User {UserId} already has confirmed email", user.Id);
            throw new ValidationException("Email is already verified");
        }

        // Verify email
        user.EmailConfirmed = true;
        emailVerificationToken.IsUsed = true;
        emailVerificationToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verified successfully for user: {UserId}", user.Id);
        return true;
    }

    public async System.Threading.Tasks.Task ResendEmailVerificationAsync(string email)
    {
        _logger.LogInformation("Attempting to resend email verification for email: {Email}", email);

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Resend email verification failed: Email is null or empty");
            throw new ValidationException("Email is required");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Resend email verification failed: User not found with email {Email}", email);
            throw new ValidationException("No account found with this email address");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogWarning("Resend email verification failed: User {UserId} already has confirmed email", user.Id);
            throw new ValidationException("Email is already verified");
        }

        // Create and send email verification token
        await CreateAndSendEmailVerificationTokenAsync(user);

        _logger.LogInformation("Email verification resent successfully for user: {UserId}", user.Id);
    }

    /// <summary>
    /// Creates and sends an email verification token for the specified user.
    /// Invalidates any existing unused tokens before creating a new one.
    /// </summary>
    /// <param name="user">The user to create the verification token for</param>
    private async System.Threading.Tasks.Task CreateAndSendEmailVerificationTokenAsync(ApplicationUser user)
    {
        // Invalidate any existing tokens for this user
        var existingTokens = await _context.EmailVerificationTokens
            .IgnoreQueryFilters() // Ignore the query filter to get all tokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();

        foreach (var token in existingTokens)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }

        // Generate new verification token
        var verificationToken = GenerateEmailVerificationToken();
        var emailVerificationToken = new EmailVerificationToken
        {
            Token = verificationToken,
            UserId = user.Id,
            Email = user.Email!,
            ExpiresAt = DateTime.UtcNow.AddHours(24) // 24 hours expiry
        };

        _context.EmailVerificationTokens.Add(emailVerificationToken);
        await _context.SaveChangesAsync();

        // Send verification email
        var verificationUrl = $"{_webAppSettings.ClientUrl}/verify-email?token={verificationToken}";
        await _emailService.SendEmailVerificationAsync(user.Email!, user.UserName!, verificationToken, verificationUrl);
    }

    private static string GenerateEmailVerificationToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // URL-safe base64
    }
    
    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[128];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
