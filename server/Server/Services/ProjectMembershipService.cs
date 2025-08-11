using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services
{
    /// <summary>
    /// Service for centralized project membership validation and role checking.
    /// </summary>
    public class ProjectMembershipService : IProjectMembershipService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly ILogger<ProjectMembershipService> _logger;

        public ProjectMembershipService(
            IProjectMemberRepository projectMemberRepository,
            ILogger<ProjectMembershipService> logger)
        {
            _projectMemberRepository = projectMemberRepository ?? throw new ArgumentNullException(nameof(projectMemberRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsProjectMemberAsync(string userId, int projectId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("IsProjectMemberAsync: UserId is null or empty");
                return false;
            }

            try
            {
                var members = await _projectMemberRepository.FindAsync(
                    pm => pm.ProjectId == projectId && pm.UserId == userId);
                
                var isMember = members.Any();
                _logger.LogDebug("User {UserId} membership in project {ProjectId}: {IsMember}", 
                    userId, projectId, isMember);
                
                return isMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project membership for user {UserId} in project {ProjectId}", 
                    userId, projectId);
                return false;
            }
        }

        public async Task<ProjectRole?> GetUserRoleInProjectAsync(string userId, int projectId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserRoleInProjectAsync: UserId is null or empty");
                return null;
            }

            try
            {
                var members = await _projectMemberRepository.FindAsync(
                    pm => pm.ProjectId == projectId && pm.UserId == userId);
                
                var member = members.FirstOrDefault();
                if (member != null)
                {
                    _logger.LogDebug("User {UserId} has role {Role} in project {ProjectId}", 
                        userId, member.Role, projectId);
                    return member.Role;
                }

                _logger.LogDebug("User {UserId} is not a member of project {ProjectId}", userId, projectId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role for user {UserId} in project {ProjectId}", 
                    userId, projectId);
                return null;
            }
        }

        public async Task<bool> HasProjectRoleAsync(string userId, int projectId, params ProjectRole[] requiredRoles)
        {
            if (string.IsNullOrEmpty(userId) || requiredRoles == null || requiredRoles.Length == 0)
            {
                _logger.LogWarning("HasProjectRoleAsync: Invalid parameters - UserId: {UserId}, RequiredRoles: {RequiredRoles}", 
                    userId, requiredRoles?.Length ?? 0);
                return false;
            }

            try
            {
                var userRole = await GetUserRoleInProjectAsync(userId, projectId);
                if (userRole == null)
                {
                    _logger.LogDebug("User {UserId} is not a member of project {ProjectId}", userId, projectId);
                    return false;
                }

                var hasRole = requiredRoles.Contains(userRole.Value);
                _logger.LogDebug("User {UserId} role check in project {ProjectId}: Has role {UserRole}, Required: [{RequiredRoles}], Result: {HasRole}", 
                    userId, projectId, userRole, string.Join(",", requiredRoles), hasRole);
                
                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project role for user {UserId} in project {ProjectId}", 
                    userId, projectId);
                return false;
            }
        }

        public async Task<IEnumerable<int>> GetUserProjectsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserProjectsAsync: UserId is null or empty");
                return Enumerable.Empty<int>();
            }

            try
            {
                var members = await _projectMemberRepository.FindAsync(pm => pm.UserId == userId);
                var projectIds = members.Select(pm => pm.ProjectId).ToList();
                
                _logger.LogDebug("User {UserId} is a member of {ProjectCount} projects: [{ProjectIds}]", 
                    userId, projectIds.Count, string.Join(",", projectIds));
                
                return projectIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects for user {UserId}", userId);
                return Enumerable.Empty<int>();
            }
        }

        public async Task<ProjectMember?> GetProjectMembershipAsync(string userId, int projectId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetProjectMembershipAsync: UserId is null or empty");
                return null;
            }

            try
            {
                var members = await _projectMemberRepository.FindAsync(
                    pm => pm.ProjectId == projectId && pm.UserId == userId);
                
                var member = members.FirstOrDefault();
                _logger.LogDebug("Project membership for user {UserId} in project {ProjectId}: {Found}", 
                    userId, projectId, member != null ? "Found" : "Not found");
                
                return member;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project membership for user {UserId} in project {ProjectId}", 
                    userId, projectId);
                return null;
            }
        }

        public async Task<bool> CanUserPerformActionAsync(string userId, int projectId, params ProjectRole[] requiredRoles)
        {
            // First check if user is a project member
            if (!await IsProjectMemberAsync(userId, projectId))
            {
                _logger.LogDebug("User {UserId} cannot perform action: not a member of project {ProjectId}", 
                    userId, projectId);
                return false;
            }

            // If no specific roles required, being a member is sufficient
            if (requiredRoles == null || requiredRoles.Length == 0)
            {
                _logger.LogDebug("User {UserId} can perform action: is member of project {ProjectId} (no role requirements)", 
                    userId, projectId);
                return true;
            }

            // Check if user has required role
            return await HasProjectRoleAsync(userId, projectId, requiredRoles);
        }
    }
}