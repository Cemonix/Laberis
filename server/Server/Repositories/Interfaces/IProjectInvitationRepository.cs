using server.Models.Domain;
using server.Repositories.Interfaces;

namespace server.Repositories.Interfaces;

public interface IProjectInvitationRepository : IGenericRepository<ProjectInvitation>
{
    Task<ProjectInvitation?> GetByTokenAsync(string token);
    Task<ProjectInvitation?> GetByEmailAndProjectAsync(string email, int projectId);
    Task<IEnumerable<ProjectInvitation>> GetExpiredInvitationsAsync();
}
