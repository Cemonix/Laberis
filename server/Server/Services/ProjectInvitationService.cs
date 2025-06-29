using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using server.Configs;
using server.Exceptions;
using server.Models.Domain;
using server.Models.DTOs.ProjectInvitation;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using System.Security.Cryptography;

namespace server.Services;

public class ProjectInvitationService : IProjectInvitationService
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly WebAppSettings _webAppSettings;
    private readonly ILogger<ProjectInvitationService> _logger;

    public ProjectInvitationService(
        IProjectInvitationRepository invitationRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectRepository projectRepository,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOptions<WebAppSettings> webAppSettings,
        ILogger<ProjectInvitationService> logger)
    {
        _invitationRepository = invitationRepository ?? throw new ArgumentNullException(nameof(invitationRepository));
        _projectMemberRepository = projectMemberRepository ?? throw new ArgumentNullException(nameof(projectMemberRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _webAppSettings = (webAppSettings ?? throw new ArgumentNullException(nameof(webAppSettings))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async System.Threading.Tasks.Task InviteUserByEmailAsync(int projectId, CreateProjectInvitationDto createDto, string inviterUserId)
    {
        _logger.LogInformation("Processing invitation for email {Email} to project {ProjectId}", createDto.Email, projectId);

        // Verify project exists
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new NotFoundException($"Project with ID {projectId} not found");
        }

        // Get inviter details
        var inviter = await _userManager.FindByIdAsync(inviterUserId);
        if (inviter == null)
        {
            throw new NotFoundException($"Inviter user with ID {inviterUserId} not found");
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(createDto.Email);
        
        if (existingUser != null)
        {
            // User exists - check if they're already a member
            var existingMembership = await _projectMemberRepository.FindAsync(
                pm => pm.ProjectId == projectId && pm.UserId == existingUser.Id);
            
            if (existingMembership.Any())
            {
                throw new ConflictException($"User with email {createDto.Email} is already a member of this project");
            }

            // Add them directly to the project
            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = existingUser.Id,
                Role = createDto.Role,
                InvitedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow, // They join immediately
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _projectMemberRepository.AddAsync(projectMember);
            await _projectMemberRepository.SaveChangesAsync();

            // Send notification email
            await _emailService.SendProjectMemberAddedEmailAsync(
                createDto.Email,
                project.Name,
                createDto.Role.ToString(),
                inviter.UserName ?? inviter.Email ?? "Someone");

            _logger.LogInformation("Added existing user {Email} to project {ProjectId}", createDto.Email, projectId);
        }
        else
        {
            // User doesn't exist - create invitation
            // Check if there's already a pending invitation
            var existingInvitation = await _invitationRepository.GetByEmailAndProjectAsync(createDto.Email, projectId);
            if (existingInvitation != null)
            {
                throw new ConflictException($"An invitation has already been sent to {createDto.Email} for this project");
            }

            var invitationToken = GenerateSecureToken();
            var invitation = new ProjectInvitation
            {
                ProjectId = projectId,
                Email = createDto.Email,
                Role = createDto.Role,
                InvitationToken = invitationToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
                IsAccepted = false,
                InvitedByUserId = inviterUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _invitationRepository.AddAsync(invitation);
            await _invitationRepository.SaveChangesAsync();

            // Send invitation email
            var registrationUrl = $"{_webAppSettings.BaseUrl}/register?inviteToken={invitationToken}";
            await _emailService.SendProjectInvitationEmailAsync(
                createDto.Email,
                project.Name,
                createDto.Role.ToString(),
                inviter.UserName ?? inviter.Email ?? "Someone",
                invitationToken,
                registrationUrl);

            _logger.LogInformation("Created invitation for new user {Email} to project {ProjectId}", createDto.Email, projectId);
        }
    }

    public async Task<bool> ProcessInvitationTokenAsync(string token, string userId)
    {
        _logger.LogInformation("Processing invitation token for user {UserId}", userId);

        var invitation = await _invitationRepository.GetByTokenAsync(token);
        if (invitation == null)
        {
            _logger.LogWarning("Invalid invitation token provided: {Token}", token);
            return false;
        }

        // Check if invitation is expired
        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Invitation token {Token} has expired", token);
            return false;
        }

        // Check if invitation is already accepted
        if (invitation.IsAccepted)
        {
            _logger.LogWarning("Invitation token {Token} has already been accepted", token);
            return false;
        }

        // Add user to project
        var projectMember = new ProjectMember
        {
            ProjectId = invitation.ProjectId,
            UserId = userId,
            Role = invitation.Role,
            InvitedAt = invitation.CreatedAt,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectMemberRepository.AddAsync(projectMember);

        // Mark invitation as accepted
        var updatedInvitation = invitation with
        {
            IsAccepted = true,
            UpdatedAt = DateTime.UtcNow
        };

        _invitationRepository.Update(updatedInvitation);
        
        await _invitationRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully processed invitation token {Token} for user {UserId}", token, userId);
        return true;
    }

    public async Task<ProjectInvitationDto?> GetValidInvitationByTokenAsync(string token)
    {
        var invitation = await _invitationRepository.GetByTokenAsync(token);
        if (invitation == null || invitation.ExpiresAt <= DateTime.UtcNow || invitation.IsAccepted)
        {
            return null;
        }

        return new ProjectInvitationDto
        {
            Id = invitation.Id,
            ProjectId = invitation.ProjectId,
            Email = invitation.Email,
            Role = invitation.Role,
            InvitationToken = invitation.InvitationToken,
            ExpiresAt = invitation.ExpiresAt,
            IsAccepted = invitation.IsAccepted,
            InvitedByUserId = invitation.InvitedByUserId,
            CreatedAt = invitation.CreatedAt
        };
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32]; // 256-bit token
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
