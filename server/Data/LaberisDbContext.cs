using System;
using Microsoft.EntityFrameworkCore;
using server.Models.Domain;
using server.Models.Domain.Enums;
using Task = server.Models.Domain.Task;

namespace server.Data;

public class LaberisDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<LabelScheme> LabelSchemes { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<WorkflowStage> WorkflowStages { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<TaskEvent> TaskEvents { get; set; }
    public DbSet<Annotation> Annotations { get; set; }
    public DbSet<Issue> Issues { get; set; }

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
        modelBuilder.ApplyConfiguration(new Configurations.DataSourceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AssetConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LabelSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LabelConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorkflowStageConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TaskConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TaskEventConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AnnotationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.IssueConfiguration());
    }
}
