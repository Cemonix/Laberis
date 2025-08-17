using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

/// <summary>
/// Entity configuration for DashboardConfiguration
/// </summary>
public class DashboardConfigurationConfiguration : IEntityTypeConfiguration<DashboardConfiguration>
{
    public void Configure(EntityTypeBuilder<DashboardConfiguration> entity)
    {
        entity.ToTable("dashboard_configurations");

        entity.HasKey(dc => dc.DashboardConfigurationId);
        entity.Property(dc => dc.DashboardConfigurationId)
            .HasColumnName("dashboard_configuration_id")
            .ValueGeneratedOnAdd();

        entity.Property(dc => dc.ConfigurationData)
            .HasColumnName("configuration_data")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        entity.Property(dc => dc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        entity.Property(dc => dc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Foreign Keys
        entity.Property(dc => dc.UserId).HasColumnName("user_id").IsRequired();
        entity.Property(dc => dc.ProjectId).HasColumnName("project_id").IsRequired();

        // Indexes for performance
        entity.HasIndex(dc => new { dc.UserId, dc.ProjectId })
            .IsUnique()
            .HasDatabaseName("idx_dashboard_configurations_user_project");

        // Relationships
        entity.HasOne(dc => dc.User)
            .WithMany()
            .HasForeignKey(dc => dc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(dc => dc.Project)
            .WithMany()
            .HasForeignKey(dc => dc.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filter to match Project entity's filter - exclude configurations for projects pending deletion
        entity.HasQueryFilter(dc => dc.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}