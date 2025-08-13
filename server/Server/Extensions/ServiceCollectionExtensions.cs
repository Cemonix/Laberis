using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Minio;
using server.Configs;
using server.Data;
using server.Repositories;
using server.Repositories.Interfaces;
using server.Services;
using server.Services.EventHandlers;
using server.Services.Interfaces;
using server.Services.Storage;
using server.Models.Domain.Enums;

namespace server.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to organize service registrations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application configuration options with validation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate Minio settings
        services.AddOptions<MinioSettings>()
            .Bind(configuration.GetSection(MinioSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate WebApp settings
        services.AddOptions<WebAppSettings>()
            .Bind(configuration.GetSection(WebAppSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate JWT settings
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate Admin User settings
        services.AddOptions<AdminUserSettings>()
            .Bind(configuration.GetSection(AdminUserSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate SMTP settings
        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate Rate Limiting settings
        services.AddOptions<RateLimitSettings>()
            .Bind(configuration.GetSection(RateLimitSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Configures Entity Framework DbContext and Identity.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        var connectionString = configuration.GetConnectionString("PostgresConnection");
        services.AddDbContext<LaberisDbContext>(options =>
        {
            options.UseNpgsql(connectionString, NpgsqlEnumMapper.ConfigureEnums);
        });

        // Configure ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<LaberisDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Registers all application repositories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<ILabelSchemeRepository, LabelSchemeRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAnnotationRepository, AnnotationRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<IProjectInvitationRepository, ProjectInvitationRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskEventRepository, TaskEventRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowStageRepository, WorkflowStageRepository>();
        services.AddScoped<IWorkflowStageConnectionRepository, WorkflowStageConnectionRepository>();
        services.AddScoped<IWorkflowStageAssignmentRepository, WorkflowStageAssignmentRepository>();

        return services;
    }

    /// <summary>
    /// Registers storage services and factories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MinIO client using AddMinio extension method
        services.AddMinio(options =>
        {
            var minioSettings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>();
            if (minioSettings != null)
            {
                options.WithEndpoint(minioSettings.Endpoint);
                options.WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey);
                options.WithSSL(minioSettings.UseSsl);
            }
        });

        services.AddScoped<IStorageServiceFactory, StorageServiceFactory>();
        services.AddScoped<IStorageService, MinioStorageService>();
        services.AddScoped<IFileStorageService, MinioStorageService>();

        return services;
    }

    /// <summary>
    /// Registers domain event services and handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEventServices(this IServiceCollection services)
    {
        // Domain Event Service (Singleton for event handling)
        services.AddSingleton<IDomainEventService, DomainEventService>();

        // Domain Event Handlers
        services.AddTransient<AssetImportedEventHandler>();

        return services;
    }

    /// <summary>
    /// Registers all business services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<ILabelSchemeService, LabelSchemeService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAnnotationService, AnnotationService>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();
        services.AddScoped<IProjectInvitationService, ProjectInvitationService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskEventService, TaskEventService>();
        services.AddScoped<ITaskStatusValidator, TaskStatusValidator>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IWorkflowStageService, WorkflowStageService>();
        services.AddScoped<IWorkflowStageConnectionService, WorkflowStageConnectionService>();
        services.AddScoped<IWorkflowStageAssignmentService, WorkflowStageAssignmentService>();
        services.AddScoped<IProjectMembershipService, ProjectMembershipService>();
        services.AddSingleton<IPermissionConfigurationService, PermissionConfigurationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}