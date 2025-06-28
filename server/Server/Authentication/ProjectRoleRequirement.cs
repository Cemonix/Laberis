using Microsoft.AspNetCore.Authorization;
using server.Models.Domain.Enums;

namespace server.Authentication
{
    public class ProjectRoleRequirement : IAuthorizationRequirement
    {
        public ProjectRole[] RequiredRoles { get; }

        public ProjectRoleRequirement(params ProjectRole[] requiredRoles)
        {
            RequiredRoles = requiredRoles;
        }
    }
}