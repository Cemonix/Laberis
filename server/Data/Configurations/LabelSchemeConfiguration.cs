using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;

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

        entity.Property(ls => ls.ProjectId).HasColumnName("project_id");

        // Unique constraint for Name within a Project
        entity.HasIndex(ls => new { ls.ProjectId, ls.Name }).IsUnique();

        // Relationship: LabelScheme to Labels (One-to-Many)
        entity.HasMany(ls => ls.Labels)
            .WithOne(l => l.LabelScheme)
            .HasForeignKey(l => l.LabelSchemeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
