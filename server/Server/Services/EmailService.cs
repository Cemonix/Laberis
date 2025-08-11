using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using server.Configs;
using server.Services.Interfaces;

namespace server.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendProjectMemberAddedEmailAsync(string email, string projectName, string role, string inviterName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = $"You've been added to '{projectName}' on Laberis";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <h2>Hello!</h2>
            <p>{inviterName} has added you to the project <strong>{projectName}</strong> on Laberis.</p>
            <p>Your role in this project is: <strong>{role}</strong></p>
            <p>You can now access the project by logging into your Laberis account.</p>
            <p>If you have any questions, please contact {inviterName} or the project administrator.</p>
            <br>
            <p>Thanks,</p>
            <p>The Laberis Team</p>";

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Project member added email sent to {EmailAddress} for project {ProjectName}", email, projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project member added email to {EmailAddress}", email);
            throw;
        }
    }

    public async Task SendProjectInvitationEmailAsync(string email, string projectName, string role, string inviterName, string invitationToken, string registrationUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = $"You've been invited to join '{projectName}' on Laberis";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <h2>Hello!</h2>
            <p>{inviterName} has invited you to join the project <strong>{projectName}</strong> on Laberis.</p>
            <p>Your role in this project will be: <strong>{role}</strong></p>
            <p>To get started, please create your account by clicking the link below:</p>
            <a href='{registrationUrl}' style='display: inline-block; padding: 12px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0;'>Create Account & Join Project</a>
            <p>This invitation will expire in 7 days.</p>
            <p>If you have any questions, please contact {inviterName}.</p>
            <br>
            <p>Thanks,</p>
            <p>The Laberis Team</p>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Project invitation email sent to {EmailAddress} for project {ProjectName}", email, projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project invitation email to {EmailAddress}", email);
            throw;
        }
    }

    public async Task SendProjectInvitationToExistingUserEmailAsync(string email, string projectName, string role, string inviterName, string invitationToken, string loginUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = $"You've been invited to join '{projectName}' on Laberis";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <h2>Hello!</h2>
            <p>{inviterName} has invited you to join the project <strong>{projectName}</strong> on Laberis.</p>
            <p>Your role in this project will be: <strong>{role}</strong></p>
            <p>Since you already have a Laberis account, please log in to accept this invitation:</p>
            <a href='{loginUrl}' style='display: inline-block; padding: 12px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0;'>Login & Accept Invitation</a>
            <p>After logging in, you'll be automatically redirected to accept the invitation.</p>
            <p>This invitation will expire in 7 days.</p>
            <p>If you have any questions, please contact {inviterName}.</p>
            <br>
            <p>Thanks,</p>
            <p>The Laberis Team</p>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Project invitation email sent to existing user {EmailAddress} for project {ProjectName}", email, projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project invitation email to existing user {EmailAddress}", email);
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(string email, string userName, string verificationToken, string verificationUrl)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Please verify your email address - Laberis";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <h2>Welcome to Laberis, {userName}!</h2>
            <p>Thank you for registering with Laberis. To complete your registration and start using your account, please verify your email address by clicking the link below:</p>
            <a href='{verificationUrl}' style='display: inline-block; padding: 12px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0;'>Verify Email Address</a>
            <p>This verification link will expire in 24 hours. If you didn't create a Laberis account, you can safely ignore this email.</p>
            <p>If you're having trouble clicking the button above, copy and paste the following URL into your web browser:</p>
            <p><a href='{verificationUrl}'>{verificationUrl}</a></p>
            <br>
            <p>Thanks,</p>
            <p>The Laberis Team</p>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Email verification sent to {EmailAddress} for user {UserName}", email, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {EmailAddress}", email);
            throw;
        }
    }
}