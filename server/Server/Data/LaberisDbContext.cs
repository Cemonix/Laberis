using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using server.Models;
using server.Models.Domain;
using server.Models.Domain.Enums;
using Task = server.Models.Domain.Task;

namespace server.Data;

public class LaberisDbContext : IdentityDbContext<ApplicationUser>
{
    public const string IdentitySchema = "identity";

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectInvitation> ProjectInvitations { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<LabelScheme> LabelSchemes { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<WorkflowStage> WorkflowStages { get; set; }
    public DbSet<WorkflowStageConnection> WorkflowStageConnections { get; set; }
    public DbSet<WorkflowStageAssignment> WorkflowStageAssignments { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<TaskEvent> TaskEvents { get; set; }
    public DbSet<Annotation> Annotations { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public DbSet<DashboardConfiguration> DashboardConfigurations { get; set; }

    public LaberisDbContext(DbContextOptions<LaberisDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ProjectType>("public", "project_type_enum");
        modelBuilder.HasPostgresEnum<ProjectStatus>("public", "project_status_enum");
        modelBuilder.HasPostgresEnum<ProjectRole>("public", "project_role_enum");
        modelBuilder.HasPostgresEnum<DataSourceType>("public", "data_source_type_enum");
        modelBuilder.HasPostgresEnum<DataSourceStatus>("public", "data_source_status_enum");
        modelBuilder.HasPostgresEnum<WorkflowStageType>("public", "workflow_stage_type_enum");
        modelBuilder.HasPostgresEnum<TaskEventType>("public", "task_event_type_enum");
        modelBuilder.HasPostgresEnum<IssueStatus>("public", "issue_status_enum");
        modelBuilder.HasPostgresEnum<IssueType>("public", "issue_type_enum");
        modelBuilder.HasPostgresEnum<AnnotationType>("public", "annotation_type_enum");
        modelBuilder.HasPostgresEnum<AssetStatus>("public", "asset_status_enum");

        // Apply configurations
        modelBuilder.ApplyConfiguration(new Configurations.ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProjectMemberConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProjectInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.DataSourceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AssetConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LabelSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LabelConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowStageConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowStageConnectionConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowStageAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TaskConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TaskEventConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AnnotationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.IssueConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.EmailVerificationTokenConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.DashboardConfigurationConfiguration());

        // Identity schema configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers", IdentitySchema);
        });
        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("AspNetRoles", IdentitySchema);
        });
        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("AspNetUserRoles", IdentitySchema);
        });
        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("AspNetUserClaims", IdentitySchema);
        });
        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("AspNetUserLogins", IdentitySchema);
        });
        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims", IdentitySchema);
        });
        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("AspNetUserTokens", IdentitySchema);
        });
    }
}
