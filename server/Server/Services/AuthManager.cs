using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using server.Configs;
using server.Exceptions;
using server.Models.DTOs.Auth;

namespace server.Services;

public class AuthManager : IAuthManager
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthManager> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _jwtSettings = (jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            EmailConfirmed = true // TODO: For now, we'll auto-confirm emails
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Registration failed for {Email}: {Errors}", registerDto.Email, errors);
            throw new ValidationException($"Registration failed: {errors}");
        }

        _logger.LogInformation("User {Email} registered successfully with ID: {UserId}", user.Email, user.Id);

        // Generate token
        var token = await GenerateTokenAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = string.Empty, // TODO: Implement refresh tokens if needed
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                CreatedAt = DateTime.UtcNow
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

        // Generate token
        var token = await GenerateTokenAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = string.Empty, // TODO: Implement refresh tokens if needed
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Expiration),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                CreatedAt = DateTime.UtcNow
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
}
