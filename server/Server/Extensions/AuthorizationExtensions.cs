using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using server.Authentication;
using server.Models.Domain.Enums;

namespace server.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies and handlers.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds application authorization policies and handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticatedUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            })
            .AddPolicy("RequireAdminRole", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Role.ADMIN.ToString());
            })
            .AddPolicy("ProjectAccess", policy =>
            {
                policy.AddRequirements(new ProjectAccessRequirement());
            })
            // Manager-only policies
            .AddPolicy("RequireManagerRole", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageProjectMembers", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageProjectSettings", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageWorkflows", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageDataSources", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            .AddPolicy("CanAccessDataExplorer", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.VIEWER, ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageLabelSchemes", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(ProjectRole.MANAGER));
            })
            // Reviewer or Manager policies
            .AddPolicy("CanReviewAnnotations", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(
                    ProjectRole.REVIEWER, ProjectRole.MANAGER));
            })
            // Annotator, Reviewer, or Manager policies  
            .AddPolicy("CanAccessAnnotationWorkspace", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(
                    ProjectRole.ANNOTATOR, ProjectRole.REVIEWER, ProjectRole.MANAGER));
            })
            .AddPolicy("CanManageAnnotations", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(
                    ProjectRole.ANNOTATOR, ProjectRole.REVIEWER, ProjectRole.MANAGER));
            })
            // Any project member policies (all roles)
            .AddPolicy("RequireProjectMembership", policy =>
            {
                policy.AddRequirements(new ProjectRoleRequirement(
                    ProjectRole.VIEWER, ProjectRole.ANNOTATOR, ProjectRole.REVIEWER, ProjectRole.MANAGER));
            });

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectRoleHandler>();
        services.AddHttpContextAccessor();

        return services;
    }
}