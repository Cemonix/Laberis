using server.Models.DTOs.Configuration;

namespace server.Models.DTOs.Auth;

public record class UserDto
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IList<string> Roles { get; init; } = [];
    
    /// <summary>
    /// User's permission context with global and project-specific permissions.
    /// Populated during login and includes all permissions available to the user.
    /// </summary>
    public UserPermissionContext? PermissionContext { get; init; }
}
