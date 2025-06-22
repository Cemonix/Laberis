using Microsoft.AspNetCore.Identity;
using server.Models.DTOs.Auth;

namespace server.Services;

public interface IAuthManager
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
