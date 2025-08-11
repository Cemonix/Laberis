using Microsoft.AspNetCore.Authorization;

namespace server.Authentication
{
    /// <summary>
    /// Authorization requirement for verifying project membership access.
    /// This requirement ensures users can only access projects they are members of.
    /// </summary>
    public class ProjectAccessRequirement : IAuthorizationRequirement
    {
        // No parameters needed - simple membership check
    }
}