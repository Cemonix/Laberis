using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;

namespace server.Data.Configurations;

public class TaskEventConfiguration : IEntityTypeConfiguration<TaskEvent>
{
    public void Configure(EntityTypeBuilder<TaskEvent> entity)
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

        // Filter for soft-deleted tasks
        entity.HasQueryFilter(te => te.Task != null && te.Task.ArchivedAt == null);

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
    }
}
