using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = server.Models.Domain.Task;

namespace server.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> entity)
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
    }
}
