using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class WorkflowStageAssignmentConfiguration : IEntityTypeConfiguration<WorkflowStageAssignment>
{
    public void Configure(EntityTypeBuilder<WorkflowStageAssignment> entity)
    {
        entity.ToTable("workflow_stage_assignments");

        entity.HasKey(wsa => wsa.WorkflowStageAssignmentId);
        entity.Property(wsa => wsa.WorkflowStageAssignmentId)
            .HasColumnName("workflow_stage_assignment_id")
            .ValueGeneratedOnAdd();

        entity.Property(wsa => wsa.WorkflowStageId).HasColumnName("workflow_stage_id").IsRequired();
        entity.Property(wsa => wsa.ProjectMemberId).HasColumnName("project_member_id").IsRequired();

        entity.Property(wsa => wsa.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        
        entity.Property(wsa => wsa.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Unique constraint to prevent duplicate assignments
        entity.HasIndex(wsa => new { wsa.WorkflowStageId, wsa.ProjectMemberId }).IsUnique();

        // Relationship: WorkflowStage (Many-to-One)
        entity.HasOne(wsa => wsa.WorkflowStage)
            .WithMany(ws => ws.StageAssignments)
            .HasForeignKey(wsa => wsa.WorkflowStageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: ProjectMember (Many-to-One)
        entity.HasOne(wsa => wsa.ProjectMember)
            .WithMany(pm => pm.WorkflowStageAssignments)
            .HasForeignKey(wsa => wsa.ProjectMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(wsa => wsa.WorkflowStage.Workflow.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
