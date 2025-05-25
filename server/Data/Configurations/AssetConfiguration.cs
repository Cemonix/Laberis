using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> entity)
    {
        entity.ToTable("assets");

        entity.HasKey(a => a.AssetId);
        entity.Property(a => a.AssetId).HasColumnName("asset_id").ValueGeneratedOnAdd();

        entity.Property(a => a.ExternalId)
            .HasColumnName("external_id")
            .IsRequired()
            .HasMaxLength(2048);

        entity.Property(a => a.Filename).HasColumnName("filename").HasMaxLength(255).IsRequired(false);
        entity.Property(a => a.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired(false);
        entity.Property(a => a.SizeBytes).HasColumnName("size_bytes").IsRequired(false);
        entity.Property(a => a.Width).HasColumnName("width").IsRequired(false);
        entity.Property(a => a.Height).HasColumnName("height").IsRequired(false);
        entity.Property(a => a.DurationMs).HasColumnName("duration_ms").IsRequired(false);

        entity.Property(a => a.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .IsRequired(false);

        entity.Property(a => a.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasDefaultValue(AssetStatus.PENDING_IMPORT)
            .HasColumnType("asset_status_enum");

        entity.Property(a => a.DeletedAt).HasColumnName("deleted_at").IsRequired(false);

        entity.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        entity.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(a => a.ProjectId).HasColumnName("project_id");
        entity.Property(a => a.DataSourceId).HasColumnName("data_source_id").IsRequired(false);

        entity.HasIndex(a => new { a.ProjectId, a.ExternalId }).IsUnique();

        // Soft delete query filter
        entity.HasQueryFilter(a => a.DeletedAt == null);

        // Relationships
        entity.HasOne(a => a.Project)
            .WithMany(p => p.Assets)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(a => a.DataSource)
            .WithMany(ds => ds.Assets)
            .HasForeignKey(a => a.DataSourceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Asset to Tasks (One-to-Many)
        entity.HasMany(a => a.Tasks)
            .WithOne(t => t.Asset)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Asset to Annotations (One-to-Many - denormalized link)
        entity.HasMany(a => a.Annotations)
            .WithOne(ann => ann.Asset)
            .HasForeignKey(ann => ann.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Asset to Issues (One-to-Many)
        entity.HasMany(a => a.Issues)
            .WithOne(i => i.Asset)
            .HasForeignKey(i => i.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
