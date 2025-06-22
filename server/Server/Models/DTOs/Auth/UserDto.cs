namespace server.Models.DTOs.Auth;

public record class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
