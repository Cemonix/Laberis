using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> entity)
    {
        entity.ToTable("data_sources");

        entity.HasKey(ds => ds.DataSourceId);
        entity.Property(ds => ds.DataSourceId).HasColumnName("data_source_id").ValueGeneratedOnAdd();

        entity.Property(ds => ds.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(ds => ds.Description)
            .HasColumnName("description")
            .IsRequired(false);

        entity.Property(ds => ds.SourceType)
            .HasColumnName("source_type")
            .IsRequired()
            .HasColumnType("public.data_source_type_enum");

        entity.Property(ds => ds.ConnectionDetails)
            .HasColumnName("connection_details")
            .HasColumnType("jsonb")
            .IsRequired(false);

        entity.Property(ds => ds.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasColumnType("public.data_source_status_enum");

        entity.Property(ds => ds.IsDefault)
            .HasColumnName("is_default")
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(ds => ds.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        entity.Property(ds => ds.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(ds => ds.ProjectId).HasColumnName("project_id");

        // Relationship: DataSource to Project (Many-to-One)
        entity.HasOne(ds => ds.Project)
            .WithMany(p => p.DataSources)
            .HasForeignKey(ds => ds.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: DataSource to Assets (One-to-Many)
        entity.HasMany(ds => ds.Assets)
            .WithOne(a => a.DataSource)
            .HasForeignKey(a => a.DataSourceId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasQueryFilter(ds => ds.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
