using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> entity)
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
            
        entity.HasQueryFilter(ds => ds.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
