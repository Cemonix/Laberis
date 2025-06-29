using server.Models.DTOs.ProjectInvitation;

namespace server.Services.Interfaces;

public interface IProjectInvitationService
{
    /// <summary>
    /// Invites a user to a project by email. Handles both existing and new users.
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="createDto">The invitation data</param>
    /// <param name="inviterUserId">The ID of the user sending the invitation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InviteUserByEmailAsync(int projectId, CreateProjectInvitationDto createDto, string inviterUserId);

    /// <summary>
    /// Validates and processes an invitation token during user registration
    /// </summary>
    /// <param name="token">The invitation token</param>
    /// <param name="userId">The ID of the newly registered user</param>
    /// <returns>A task representing the asynchronous operation, returns true if processed successfully</returns>
    Task<bool> ProcessInvitationTokenAsync(string token, string userId);

    /// <summary>
    /// Gets a valid invitation by token
    /// </summary>
    /// <param name="token">The invitation token</param>
    /// <returns>The invitation DTO if valid, null otherwise</returns>
    Task<ProjectInvitationDto?> GetValidInvitationByTokenAsync(string token);
}
