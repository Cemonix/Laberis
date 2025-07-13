using Microsoft.AspNetCore.Authorization;
using server.Repositories.Interfaces;
using System.Security.Claims;
using server.Models.Domain;
using Task = System.Threading.Tasks.Task;

namespace server.Authentication
{
    public class ProjectRoleHandler : AuthorizationHandler<ProjectRoleRequirement>
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProjectRoleHandler(IProjectMemberRepository projectMemberRepository, IHttpContextAccessor httpContextAccessor)
        {
            _projectMemberRepository = projectMemberRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectRoleRequirement requirement)
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // Get the current user's ID from the token claims
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            // Get the projectId from the route values
            if (!int.TryParse(httpContext.GetRouteValue("projectId")?.ToString(), out int projectId))
            {
                context.Fail();
                return;
            }

            // Find the user's membership details for this specific project
            IEnumerable<ProjectMember> projectMembers = await _projectMemberRepository.FindAsync(
                pm => pm.ProjectId == projectId && pm.UserId == userId
            );
            ProjectMember? projectMember = projectMembers.FirstOrDefault();

            if (projectMember != null && requirement.RequiredRoles.Contains(projectMember.Role))
            {
                // User has one of the required roles for this project
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}