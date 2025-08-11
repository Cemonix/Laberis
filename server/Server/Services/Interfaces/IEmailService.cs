namespace server.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an invitation email to a user who already exists in the system
    /// </summary>
    /// <param name="email">The recipient's email address</param>
    /// <param name="projectName">Name of the project they're being added to</param>
    /// <param name="role">Their role in the project</param>
    /// <param name="inviterName">Name of the person who invited them</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendProjectMemberAddedEmailAsync(string email, string projectName, string role, string inviterName);

    /// <summary>
    /// Sends an invitation email to a user who doesn't exist in the system yet
    /// </summary>
    /// <param name="email">The recipient's email address</param>
    /// <param name="projectName">Name of the project they're being invited to</param>
    /// <param name="role">Their role in the project</param>
    /// <param name="inviterName">Name of the person who invited them</param>
    /// <param name="invitationToken">The unique invitation token for registration</param>
    /// <param name="registrationUrl">The frontend registration URL with the token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendProjectInvitationEmailAsync(string email, string projectName, string role, string inviterName, string invitationToken, string registrationUrl);

    /// <summary>
    /// Sends an invitation email to an existing user who needs to accept the invitation
    /// </summary>
    /// <param name="email">The recipient's email address</param>
    /// <param name="projectName">Name of the project they're being invited to</param>
    /// <param name="role">Their role in the project</param>
    /// <param name="inviterName">Name of the person who invited them</param>
    /// <param name="invitationToken">The unique invitation token</param>
    /// <param name="loginUrl">The frontend login/acceptance URL with the token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendProjectInvitationToExistingUserEmailAsync(string email, string projectName, string role, string inviterName, string invitationToken, string loginUrl);

    /// <summary>
    /// Sends an email verification email to a newly registered user
    /// </summary>
    /// <param name="email">The recipient's email address</param>
    /// <param name="userName">The user's username</param>
    /// <param name="verificationToken">The unique verification token</param>
    /// <param name="verificationUrl">The frontend verification URL with the token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendEmailVerificationAsync(string email, string userName, string verificationToken, string verificationUrl);
}