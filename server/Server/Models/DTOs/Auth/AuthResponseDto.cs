namespace server.Models.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
    public string? RefreshToken { get; set; } = null; // Only populated for refresh operations
}
