using server.Models.DTOs.Auth;

namespace server.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<string> GenerateTokenAsync(ApplicationUser user);
    Task<AuthResponseDto> RefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task SendEmailVerificationAsync(string userId);
    Task<bool> VerifyEmailAsync(string token);
    Task ResendEmailVerificationAsync(string email);
}
