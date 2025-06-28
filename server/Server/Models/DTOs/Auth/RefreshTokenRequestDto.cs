namespace server.Models.DTOs.Auth;

public record class RefreshTokenRequestDto
{
    /// <summary>
    /// The refresh token string.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// The expiration time of the refresh token.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// The user ID associated with the refresh token.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}