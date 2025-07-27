using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class LabelSchemeConfiguration : IEntityTypeConfiguration<LabelScheme>
{
    public void Configure(EntityTypeBuilder<LabelScheme> entity)
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

        entity.Property(ls => ls.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        entity.Property(ls => ls.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        entity.Property(ls => ls.ProjectId).HasColumnName("project_id");

        // Unique constraint for Name within a Project (only for active schemes)
        entity.HasIndex(ls => new { ls.ProjectId, ls.Name, ls.IsActive })
            .IsUnique()
            .HasFilter("is_active = true");

        // Relationship: LabelScheme to Labels (One-to-Many)
        entity.HasMany(ls => ls.Labels)
            .WithOne(l => l.LabelScheme)
            .HasForeignKey(l => l.LabelSchemeId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(ds => ds.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
