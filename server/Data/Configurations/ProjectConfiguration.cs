using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
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
    
    }
}
