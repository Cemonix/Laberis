using Microsoft.AspNetCore.Authorization;
using server.Repositories.Interfaces;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace server.Authentication
{
    /// <summary>
    /// Authorization handler that validates project membership access.
    /// Ensures users can only access projects they are members of.
    /// </summary>
    public class ProjectAccessHandler : AuthorizationHandler<ProjectAccessRequirement>
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProjectAccessHandler> _logger;

        public ProjectAccessHandler(
            IProjectMemberRepository projectMemberRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ProjectAccessHandler> logger)
        {
            _projectMemberRepository = projectMemberRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ProjectAccessRequirement requirement)
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null");
                context.Fail();
                return;
            }

            // Get the current user's ID from the token claims
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                context.Fail();
                return;
            }

            // Try to get projectId from route values
            var projectIdValue = httpContext.GetRouteValue("projectId")?.ToString();
            if (string.IsNullOrEmpty(projectIdValue) || !int.TryParse(projectIdValue, out int projectId))
            {
                _logger.LogWarning("Project ID not found or invalid in route for user {UserId}", userId);
                context.Fail();
                return;
            }

            try
            {
                // Check if user is a member of this project
                var projectMembers = await _projectMemberRepository.FindAsync(
                    pm => pm.ProjectId == projectId && pm.UserId == userId
                );

                var projectMember = projectMembers.FirstOrDefault();
                if (projectMember != null)
                {
                    _logger.LogDebug("User {UserId} has access to project {ProjectId} with role {Role}", 
                        userId, projectId, projectMember.Role);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User {UserId} attempted to access project {ProjectId} without membership", 
                        userId, projectId);
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project membership for user {UserId} and project {ProjectId}", 
                    userId, projectId);
                context.Fail();
            }
        }
    }
}