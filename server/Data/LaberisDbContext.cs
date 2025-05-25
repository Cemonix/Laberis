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

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.HasKey(p => p.ProjectId);
            entity.Property(p => p.ProjectId).HasColumnName("project_id").ValueGeneratedOnAdd();

            entity.Property(p => p.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(p => p.ProjectType)
                .HasColumnName("project_type")
                .IsRequired()
                .HasDefaultValue(ProjectType.OTHER)
                .HasColumnType("project_type_enum");

            entity.Property(p => p.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(ProjectStatus.ACTIVE)
                .HasColumnType("project_status_enum");

            entity.Property(p => p.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired(false);

            entity.Property(p => p.AnnotationGuidelinesUrl)
                .HasColumnName("annotation_guidelines_url")
                .IsRequired(false);

            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Relationships
            entity.HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many relationships
            entity.HasMany(p => p.LabelSchemes)
                .WithOne(l => l.Project)
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.DataSources)
                .WithOne(ds => ds.Project)
                .HasForeignKey(ds => ds.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Assets)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Workflows)
                .WithOne(w => w.Project)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.ProjectMembers)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("project_members");

            entity.HasKey(pm => pm.ProjectMemberId);
            entity.Property(pm => pm.ProjectMemberId).HasColumnName("project_member_id").ValueGeneratedOnAdd();

            entity.Property(pm => pm.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasColumnType("project_role_enum");

            entity.Property(pm => pm.InvitedAt)
                .HasColumnName("invited_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(pm => pm.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired(false);

            entity.Property(pm => pm.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(pm => pm.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(pm => pm.ProjectId).HasColumnName("project_id");
            entity.Property(pm => pm.UserId).HasColumnName("user_id").IsRequired();

            // Unique constraint for (ProjectId, UserId)
            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

            // Relationship: ProjectMember to Project (Many-to-One)
            entity.HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ProjectMember to User (Many-to-One)
            entity.HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.ToTable("data_sources");

            entity.HasKey(ds => ds.DataSourceId);
            entity.Property(ds => ds.DataSourceId).HasColumnName("data_source_id").ValueGeneratedOnAdd();

            entity.Property(ds => ds.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ds => ds.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(ds => ds.SourceType)
                .HasColumnName("source_type")
                .IsRequired()
                .HasColumnType("data_source_type_enum");

            entity.Property(ds => ds.ConnectionDetails)
                .HasColumnName("connection_details")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(ds => ds.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(DataSourceStatus.ACTIVE)
                .HasColumnType("data_source_status_enum");

            entity.Property(ds => ds.IsDefault)
                .HasColumnName("is_default")
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(ds => ds.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(ds => ds.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ds => ds.ProjectId).HasColumnName("project_id");

            // Relationship: DataSource to Project (Many-to-One)
            entity.HasOne(ds => ds.Project)
                .WithMany(p => p.DataSources)
                .HasForeignKey(ds => ds.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: DataSource to Assets (One-to-Many)
            entity.HasMany(ds => ds.Assets)
                .WithOne(a => a.DataSource)
                .HasForeignKey(a => a.DataSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.ToTable("workflows");

            entity.HasKey(w => w.WorkflowId);
            entity.Property(w => w.WorkflowId).HasColumnName("workflow_id").ValueGeneratedOnAdd();

            entity.Property(w => w.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(w => w.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(w => w.IsDefault)
                .HasColumnName("is_default")
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(w => w.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(w => w.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(w => w.ProjectId).HasColumnName("project_id");

            // Unique constraint for Name within a Project
            entity.HasIndex(w => new { w.ProjectId, w.Name }).IsUnique();

            // Relationship: Workflow to Project (Many-to-One)
            entity.HasOne(w => w.Project)
                .WithMany(p => p.Workflows)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Workflow to WorkflowStages (One-to-Many)
            entity.HasMany(w => w.WorkflowStages)
                .WithOne(ws => ws.Workflow) 
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Workflow to Tasks (One-to-Many)
            entity.HasMany(w => w.Tasks)
                .WithOne(t => t.Workflow)
                .HasForeignKey(t => t.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowStage>(entity =>
        {
            entity.ToTable("workflow_stages");

            entity.HasKey(ws => ws.WorkflowStageId);
            entity.Property(ws => ws.WorkflowStageId).HasColumnName("workflow_stage_id").ValueGeneratedOnAdd();

            entity.Property(ws => ws.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ws => ws.Description).HasColumnName("description").IsRequired(false);
            entity.Property(ws => ws.StageOrder).HasColumnName("stage_order").IsRequired();

            entity.Property(ws => ws.StageType)
                .HasColumnName("stage_type")
                .IsRequired(false)
                .HasColumnType("workflow_stage_type_enum");

            entity.Property(ws => ws.IsInitialStage).HasColumnName("is_initial_stage").IsRequired().HasDefaultValue(false);
            entity.Property(ws => ws.IsFinalStage).HasColumnName("is_final_stage").IsRequired().HasDefaultValue(false);

            entity.Property(ws => ws.UiConfiguration)
                .HasColumnName("ui_configuration")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(ws => ws.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
            entity.Property(ws => ws.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ws => ws.WorkflowId).HasColumnName("workflow_id");

            entity.Property(ws => ws.InputDataSourceId)
                .HasColumnName("input_data_source_id")
                .IsRequired(false);
            entity.Property(ws => ws.TargetDataSourceId)
                .HasColumnName("target_data_source_id")
                .IsRequired(false);

            entity.HasIndex(ws => new { ws.WorkflowId, ws.Name }).IsUnique();
            entity.HasIndex(ws => new { ws.WorkflowId, ws.StageOrder }).IsUnique();

            // Relationship: WorkflowStage to Workflow (Many-to-One)
            entity.HasOne(ws => ws.Workflow)
                .WithMany(w => w.WorkflowStages)
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ws => ws.InputDataSource)
                .WithMany()
                .HasForeignKey(ws => ws.InputDataSourceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Relationship: WorkflowStage to TargetDataSource (Many-to-One, Optional)
            entity.HasOne(ws => ws.TargetDataSource)
                .WithMany()
                .HasForeignKey(ws => ws.TargetDataSourceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Relationship: WorkflowStage to Tasks (One-to-Many where this stage is current)
            entity.HasMany(ws => ws.TasksAtThisStage)
                .WithOne(t => t.CurrentWorkflowStage)
                .HasForeignKey(t => t.CurrentWorkflowStageId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // New Entity Configuration for Task
        modelBuilder.Entity<Task>(entity =>
        {
            entity.ToTable("tasks");

            entity.HasKey(t => t.TaskId);
            entity.Property(t => t.TaskId).HasColumnName("task_id").ValueGeneratedOnAdd();

            entity.Property(t => t.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(0);
            entity.Property(t => t.Status).HasColumnName("status").HasMaxLength(50).IsRequired(false).HasDefaultValue("OPEN");
            entity.Property(t => t.DueDate).HasColumnName("due_date").IsRequired(false);
            entity.Property(t => t.Metadata).HasColumnName("metadata").HasColumnType("jsonb").IsRequired(false);
            entity.Property(t => t.CompletedAt).HasColumnName("completed_at").IsRequired(false);
            entity.Property(t => t.ArchivedAt).HasColumnName("archived_at").IsRequired(false);

            entity.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
            entity.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(t => t.AssetId).HasColumnName("asset_id");
            entity.Property(t => t.ProjectId).HasColumnName("project_id");
            entity.Property(t => t.WorkflowId).HasColumnName("workflow_id");
            entity.Property(t => t.CurrentWorkflowStageId).HasColumnName("current_workflow_stage_id");
            entity.Property(t => t.AssignedToUserId).HasColumnName("assigned_to_user_id").IsRequired(false);
            entity.Property(t => t.LastWorkedOnByUserId).HasColumnName("last_worked_on_by_user_id").IsRequired(false);

            // Relationships for Task
            entity.HasOne(t => t.Asset)
                .WithMany()
                .HasForeignKey(t => t.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Project)
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Workflow)
                .WithMany(w => w.Tasks)
                .HasForeignKey(t => t.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.CurrentWorkflowStage)
                .WithMany(ws => ws.TasksAtThisStage)
                .HasForeignKey(t => t.CurrentWorkflowStageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.LastWorkedOnByUser)
                .WithMany()
                .HasForeignKey(t => t.LastWorkedOnByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many from Task to its child entities
            entity.HasMany(t => t.Annotations)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.TaskEvents)
                .WithOne(te => te.Task)
                .HasForeignKey(te => te.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Issues)
                .WithOne(i => i.Task)
                .HasForeignKey(i => i.TaskId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskEvent>(entity =>
        {
            entity.ToTable("task_events");

            entity.HasKey(te => te.EventId);
            entity.Property(te => te.EventId).HasColumnName("event_id").ValueGeneratedOnAdd();

            entity.Property(te => te.EventType)
                .HasColumnName("event_type")
                .IsRequired()
                .HasColumnType("task_event_type_enum");

            entity.Property(te => te.Details)
                .HasColumnName("details")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(te => te.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(te => te.TaskId).HasColumnName("task_id");
            entity.Property(te => te.UserId).HasColumnName("user_id").IsRequired(false);
            entity.Property(te => te.FromWorkflowStageId).HasColumnName("from_workflow_stage_id").IsRequired(false);
            entity.Property(te => te.ToWorkflowStageId).HasColumnName("to_workflow_stage_id").IsRequired(false);

            // Relationships
            entity.HasOne(te => te.Task)
                .WithMany(t => t.TaskEvents)
                .HasForeignKey(te => te.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(te => te.User)
                .WithMany()
                .HasForeignKey(te => te.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(te => te.FromWorkflowStage)
                .WithMany()
                .HasForeignKey(te => te.FromWorkflowStageId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(te => te.ToWorkflowStage)
                .WithMany()
                .HasForeignKey(te => te.ToWorkflowStageId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("assets");

            entity.HasKey(a => a.AssetId);
            entity.Property(a => a.AssetId).HasColumnName("asset_id").ValueGeneratedOnAdd();

            entity.Property(a => a.ExternalId)
                .HasColumnName("external_id")
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(a => a.Filename).HasColumnName("filename").HasMaxLength(255).IsRequired(false);
            entity.Property(a => a.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired(false);
            entity.Property(a => a.SizeBytes).HasColumnName("size_bytes").IsRequired(false);
            entity.Property(a => a.Width).HasColumnName("width").IsRequired(false);
            entity.Property(a => a.Height).HasColumnName("height").IsRequired(false);
            entity.Property(a => a.DurationMs).HasColumnName("duration_ms").IsRequired(false);

            entity.Property(a => a.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(a => a.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(AssetStatus.PENDING_IMPORT)
                .HasColumnType("asset_status_enum");

            entity.Property(a => a.DeletedAt).HasColumnName("deleted_at").IsRequired(false);

            entity.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
            entity.Property(a => a.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(a => a.ProjectId).HasColumnName("project_id");
            entity.Property(a => a.DataSourceId).HasColumnName("data_source_id").IsRequired(false);

            entity.HasIndex(a => new { a.ProjectId, a.ExternalId }).IsUnique();

            // Soft delete query filter
            entity.HasQueryFilter(a => a.DeletedAt == null);

            // Relationships
            entity.HasOne(a => a.Project)
                .WithMany(p => p.Assets)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.DataSource)
                .WithMany(ds => ds.Assets)
                .HasForeignKey(a => a.DataSourceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Asset to Tasks (One-to-Many)
            entity.HasMany(a => a.Tasks)
                .WithOne(t => t.Asset)
                .HasForeignKey(t => t.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Asset to Annotations (One-to-Many - denormalized link)
            entity.HasMany(a => a.Annotations)
                .WithOne(ann => ann.Asset)
                .HasForeignKey(ann => ann.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Asset to Issues (One-to-Many)
            entity.HasMany(a => a.Issues)
                .WithOne(i => i.Asset)
                .HasForeignKey(i => i.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Annotation>(entity =>
        {
            entity.ToTable("annotations");

            entity.HasKey(ann => ann.AnnotationId);
            entity.Property(ann => ann.AnnotationId).HasColumnName("annotation_id").ValueGeneratedOnAdd();

            entity.Property(ann => ann.AnnotationType)
                .HasColumnName("annotation_type")
                .IsRequired()
                .HasColumnType("annotation_type_enum");

            entity.Property(ann => ann.Data)
                .HasColumnName("data")
                .HasColumnType("jsonb") // Assuming TEXT in DB for JSON string
                .IsRequired();

            entity.Property(ann => ann.IsPrediction).HasColumnName("is_prediction").IsRequired().HasDefaultValue(false);
            entity.Property(ann => ann.ConfidenceScore).HasColumnName("confidence_score").IsRequired(false);
            entity.Property(ann => ann.IsGroundTruth).HasColumnName("is_ground_truth").IsRequired().HasDefaultValue(false);
            entity.Property(ann => ann.Version).HasColumnName("version").IsRequired().HasDefaultValue(1);
            entity.Property(ann => ann.Notes).HasColumnName("notes").IsRequired(false);

            entity.Property(ann => ann.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
            entity.Property(ann => ann.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ann => ann.TaskId).HasColumnName("task_id");
            entity.Property(ann => ann.AssetId).HasColumnName("asset_id");
            entity.Property(ann => ann.LabelId).HasColumnName("label_id");
            entity.Property(ann => ann.AnnotatorUserId).HasColumnName("annotator_user_id").IsRequired();
            entity.Property(ann => ann.ParentAnnotationId).HasColumnName("parent_annotation_id").IsRequired(false);

            // Relationships
            entity.HasOne(ann => ann.Task)
                .WithMany(t => t.Annotations)
                .HasForeignKey(ann => ann.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ann => ann.Asset)
                .WithMany(a => a.Annotations)
                .HasForeignKey(ann => ann.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ann => ann.Label)
                .WithMany()
                .HasForeignKey(ann => ann.LabelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ann => ann.AnnotatorUser)
                .WithMany()
                .HasForeignKey(ann => ann.AnnotatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ann => ann.ParentAnnotation)
                .WithMany(pa => pa.ChildAnnotations)
                .HasForeignKey(ann => ann.ParentAnnotationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Annotation to Issues (One-to-Many)
            entity.HasMany(ann => ann.Issues)
                .WithOne(i => i.Annotation)
                .HasForeignKey(i => i.AnnotationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.ToTable("issues");

            entity.HasKey(i => i.IssueId);
            entity.Property(i => i.IssueId).HasColumnName("issue_id").ValueGeneratedOnAdd();

            entity.Property(i => i.Description)
                .HasColumnName("description")
                .IsRequired();

            entity.Property(i => i.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(IssueStatus.OPEN)
                .HasColumnType("issue_status_enum");

            entity.Property(i => i.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(0);

            entity.Property(i => i.IssueType)
                .HasColumnName("issue_type")
                .IsRequired(false)
                .HasColumnType("issue_type_enum");

            entity.Property(i => i.ResolutionDetails).HasColumnName("resolution_details").IsRequired(false);
            entity.Property(i => i.ResolvedAt).HasColumnName("resolved_at").IsRequired(false);

            entity.Property(i => i.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
            entity.Property(i => i.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(i => i.TaskId).HasColumnName("task_id").IsRequired(false);
            entity.Property(i => i.AssetId).HasColumnName("asset_id").IsRequired();
            entity.Property(i => i.AnnotationId).HasColumnName("annotation_id").IsRequired(false);
            entity.Property(i => i.ReportedByUserId).HasColumnName("reported_by_user_id").IsRequired();
            entity.Property(i => i.AssignedToUserId).HasColumnName("assigned_to_user_id").IsRequired(false);

            // Relationships
            entity.HasOne(i => i.Task)
                .WithMany(t => t.Issues)
                .HasForeignKey(i => i.TaskId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(i => i.Asset)
                .WithMany()
                .HasForeignKey(i => i.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Annotation)
                .WithMany()
                .HasForeignKey(i => i.AnnotationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(i => i.ReportedByUser)
                .WithMany()
                .HasForeignKey(i => i.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.AssignedToUser)
                .WithMany()
                .HasForeignKey(i => i.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<LabelScheme>(entity =>
        {
            entity.ToTable("label_schemes");

            entity.HasKey(ls => ls.LabelSchemeId);
            entity.Property(ls => ls.LabelSchemeId).HasColumnName("label_scheme_id").ValueGeneratedOnAdd();

            entity.Property(ls => ls.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ls => ls.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(ls => ls.IsDefault)
                .HasColumnName("is_default")
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(ls => ls.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(ls => ls.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ls => ls.ProjectId).HasColumnName("project_id");

            // Unique constraint for Name within a Project
            entity.HasIndex(ls => new { ls.ProjectId, ls.Name }).IsUnique();

            // Relationship: LabelScheme to Labels (One-to-Many)
            entity.HasMany(ls => ls.Labels)
                .WithOne(l => l.LabelScheme)
                .HasForeignKey(l => l.LabelSchemeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("labels");

            entity.HasKey(l => l.LabelId);
            entity.Property(l => l.LabelId).HasColumnName("label_id").ValueGeneratedOnAdd();

            entity.Property(l => l.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(l => l.Color)
                .HasColumnName("color")
                .HasMaxLength(7) // For #RRGGBB
                .IsRequired(false);

            entity.Property(l => l.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(l => l.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(l => l.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(l => l.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(l => l.LabelSchemeId).HasColumnName("label_scheme_id");

            // Unique constraint for Name within a LabelScheme
            entity.HasIndex(l => new { l.LabelSchemeId, l.Name }).IsUnique();
        });
    }
}
