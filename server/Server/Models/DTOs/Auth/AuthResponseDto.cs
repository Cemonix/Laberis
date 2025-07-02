namespace server.Models.DTOs.Auth;

public record class AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}
