using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class WorkflowStageConnectionConfiguration : IEntityTypeConfiguration<WorkflowStageConnection>
{
    public void Configure(EntityTypeBuilder<WorkflowStageConnection> entity)
    {
        entity.ToTable("workflow_stage_connections");

        entity.HasKey(wsc => wsc.WorkflowStageConnectionId);
        entity.Property(wsc => wsc.WorkflowStageConnectionId)
            .HasColumnName("workflow_stage_connection_id")
            .ValueGeneratedOnAdd();

        entity.Property(wsc => wsc.FromStageId).HasColumnName("from_stage_id").IsRequired();
        entity.Property(wsc => wsc.ToStageId).HasColumnName("to_stage_id").IsRequired();
        
        entity.Property(wsc => wsc.Condition)
            .HasColumnName("condition")
            .HasMaxLength(255)
            .IsRequired(false);

        entity.Property(wsc => wsc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        
        entity.Property(wsc => wsc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Unique constraint to prevent duplicate connections
        entity.HasIndex(wsc => new { wsc.FromStageId, wsc.ToStageId, wsc.Condition }).IsUnique();

        // Relationship: FromStage (Many-to-One)
        entity.HasOne(wsc => wsc.FromStage)
            .WithMany(ws => ws.OutgoingConnections)
            .HasForeignKey(wsc => wsc.FromStageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: ToStage (Many-to-One)
        entity.HasOne(wsc => wsc.ToStage)
            .WithMany(ws => ws.IncomingConnections)
            .HasForeignKey(wsc => wsc.ToStageId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(wsc => wsc.FromStage.Workflow.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
