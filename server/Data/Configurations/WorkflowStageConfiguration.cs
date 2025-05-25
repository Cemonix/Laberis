using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;

namespace server.Data.Configurations;

public class WorkflowStageConfiguration : IEntityTypeConfiguration<WorkflowStage>
{
    public void Configure(EntityTypeBuilder<WorkflowStage> entity)
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
    }
}
