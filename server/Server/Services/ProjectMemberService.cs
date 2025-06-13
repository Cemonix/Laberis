using server.Models.Domain;
using server.Models.DTOs.ProjectMember;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class ProjectMemberService : IProjectMemberService
{
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ILogger<ProjectMemberService> _logger;

    public ProjectMemberService(
        IProjectMemberRepository projectMemberRepository,
        ILogger<ProjectMemberService> logger)
    {
        _projectMemberRepository = projectMemberRepository ?? throw new ArgumentNullException(nameof(projectMemberRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching project members for project: {ProjectId}", projectId);
        
        var members = await _projectMemberRepository.GetAllAsync(
            filter: pm => pm.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} project members for project: {ProjectId}", members.Count(), projectId);
        
        return members.Select(MapToDto);
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetUserProjectMembershipsAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching project memberships for user: {UserId}", userId);
        
        var memberships = await _projectMemberRepository.GetAllAsync(
            filter: pm => pm.UserId == userId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} project memberships for user: {UserId}", memberships.Count(), userId);
        
        return memberships.Select(MapToDto);
    }

    public async Task<ProjectMemberDto?> GetProjectMemberAsync(int projectId, string userId)
    {
        _logger.LogInformation("Fetching project member for project: {ProjectId}, user: {UserId}", projectId, userId);
        
        var members = await _projectMemberRepository.FindAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        var member = members.FirstOrDefault();
        
        if (member == null)
        {
            _logger.LogWarning("Project member not found for project: {ProjectId}, user: {UserId}", projectId, userId);
            return null;
        }

        return MapToDto(member);
    }

    public async Task<ProjectMemberDto> AddProjectMemberAsync(int projectId, CreateProjectMemberDto createDto)
    {
        _logger.LogInformation("Adding new member to project: {ProjectId}", projectId);

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = createDto.UserId,
            Role = createDto.Role,
            InvitedAt = DateTime.UtcNow,
            JoinedAt = null, // Will be set after the member accepts the invitation
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectMemberRepository.AddAsync(member);
        await _projectMemberRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully added member with ID: {MemberId} to project: {ProjectId}", member.ProjectMemberId, projectId);
        
        return MapToDto(member);
    }

    public async Task<ProjectMemberDto?> UpdateProjectMemberAsync(int projectId, string userId, UpdateProjectMemberDto updateDto)
    {
        _logger.LogInformation("Updating project member for project: {ProjectId}, user: {UserId}", projectId, userId);

        var members = await _projectMemberRepository.FindAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        var member = members.FirstOrDefault();
        
        if (member == null)
        {
            _logger.LogWarning("Project member not found for update. Project: {ProjectId}, user: {UserId}", projectId, userId);
            return null;
        }

        var updatedMember = member with
        {
            Role = updateDto.Role,
            UpdatedAt = DateTime.UtcNow
        };

        _projectMemberRepository.Update(updatedMember);
        await _projectMemberRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated project member for project: {ProjectId}, user: {UserId}", projectId, userId);
        
        return MapToDto(updatedMember);
    }

    public async Task<bool> RemoveProjectMemberAsync(int projectId, string userId)
    {
        _logger.LogInformation("Removing project member from project: {ProjectId}, user: {UserId}", projectId, userId);

        var members = await _projectMemberRepository.FindAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        var member = members.FirstOrDefault();
        
        if (member == null)
        {
            _logger.LogWarning("Project member not found for removal. Project: {ProjectId}, user: {UserId}", projectId, userId);
            return false;
        }

        _projectMemberRepository.Remove(member);
        await _projectMemberRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully removed project member from project: {ProjectId}, user: {UserId}", projectId, userId);
        
        return true;
    }

    private static ProjectMemberDto MapToDto(ProjectMember member)
    {
        return new ProjectMemberDto
        {
            Id = member.ProjectMemberId,
            Role = member.Role,
            InvitedAt = member.InvitedAt,
            JoinedAt = member.JoinedAt,
            CreatedAt = member.CreatedAt,
            ProjectId = member.ProjectId,
            UserId = member.UserId
        };
    }
}
