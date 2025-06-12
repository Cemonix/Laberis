using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> entity)
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

        entity.HasQueryFilter(l => l.LabelScheme.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
