using Microsoft.AspNetCore.Authorization;

namespace server.Authentication
{
    /// <summary>
    /// Authorization attribute that ensures users can only access projects they are members of.
    /// This attribute validates basic project membership only.
    /// 
    /// For role-based access control, use the existing [Authorize(Policy = "CanManageProjectMembers")] pattern.
    /// 
    /// Usage:
    /// [ProjectAccess] - Validates project membership only
    /// </summary>
    public class ProjectAccessAttribute : AuthorizeAttribute
    {
        public ProjectAccessAttribute()
        {
            // Use the ProjectAccess policy for basic membership validation
            Policy = "ProjectAccess";
        }
    }
}