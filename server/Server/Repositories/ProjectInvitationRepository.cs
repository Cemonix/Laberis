using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;

namespace server.Repositories;

public class ProjectInvitationRepository : GenericRepository<ProjectInvitation>, IProjectInvitationRepository
{
    public ProjectInvitationRepository(LaberisDbContext context) : base(context)
    {
    }

    public async Task<ProjectInvitation?> GetByTokenAsync(string token)
    {
        return await _context.Set<ProjectInvitation>()
            .IgnoreQueryFilters() // Include expired and accepted invitations for validation
            .Include(pi => pi.Project)
            .FirstOrDefaultAsync(pi => pi.InvitationToken == token);
    }

    public async Task<ProjectInvitation?> GetByEmailAndProjectAsync(string email, int projectId)
    {
        return await _context.Set<ProjectInvitation>()
            .FirstOrDefaultAsync(pi => pi.Email == email && pi.ProjectId == projectId);
    }

    public async Task<IEnumerable<ProjectInvitation>> GetExpiredInvitationsAsync()
    {
        return await _context.Set<ProjectInvitation>()
            .IgnoreQueryFilters()
            .Where(pi => pi.ExpiresAt <= DateTime.UtcNow && !pi.IsAccepted)
            .ToListAsync();
    }

    protected override IQueryable<ProjectInvitation> ApplyIncludes(IQueryable<ProjectInvitation> query)
    {
        return query
            .Include(pi => pi.Project)
            .Include(pi => pi.InvitedByUser);
    }

    protected override IQueryable<ProjectInvitation> ApplyFilter(IQueryable<ProjectInvitation> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        return filterOn.ToLower() switch
        {
            "email" => query.Where(pi => pi.Email.ToLower().Contains(filterQuery.ToLower())),
            "role" => query.Where(pi => pi.Role.ToString().ToLower().Contains(filterQuery.ToLower())),
            "project_id" => int.TryParse(filterQuery, out var projectId) 
                ? query.Where(pi => pi.ProjectId == projectId) 
                : query,
            "is_accepted" => bool.TryParse(filterQuery, out var isAccepted) 
                ? query.Where(pi => pi.IsAccepted == isAccepted) 
                : query,
            "invited_by_user_id" => query.Where(pi => pi.InvitedByUserId == filterQuery),
            _ => query
        };
    }

    protected override IQueryable<ProjectInvitation> ApplySorting(IQueryable<ProjectInvitation> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return isAscending 
                ? query.OrderBy(pi => pi.CreatedAt) 
                : query.OrderByDescending(pi => pi.CreatedAt);
        }

        return sortBy.ToLower() switch
        {
            "email" => isAscending 
                ? query.OrderBy(pi => pi.Email) 
                : query.OrderByDescending(pi => pi.Email),
            "role" => isAscending 
                ? query.OrderBy(pi => pi.Role) 
                : query.OrderByDescending(pi => pi.Role),
            "project_id" => isAscending 
                ? query.OrderBy(pi => pi.ProjectId) 
                : query.OrderByDescending(pi => pi.ProjectId),
            "expires_at" => isAscending 
                ? query.OrderBy(pi => pi.ExpiresAt) 
                : query.OrderByDescending(pi => pi.ExpiresAt),
            "is_accepted" => isAscending 
                ? query.OrderBy(pi => pi.IsAccepted) 
                : query.OrderByDescending(pi => pi.IsAccepted),
            "created_at" => isAscending 
                ? query.OrderBy(pi => pi.CreatedAt) 
                : query.OrderByDescending(pi => pi.CreatedAt),
            _ => isAscending 
                ? query.OrderBy(pi => pi.CreatedAt) 
                : query.OrderByDescending(pi => pi.CreatedAt)
        };
    }
}
