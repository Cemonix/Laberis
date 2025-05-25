using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> entity)
    {
        entity.ToTable("issues");

        entity.HasKey(i => i.IssueId);
        entity.Property(i => i.IssueId).HasColumnName("issue_id").ValueGeneratedOnAdd();

        entity.Property(i => i.Description)
            .HasColumnName("description")
            .IsRequired();

        entity.Property(i => i.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasDefaultValue(IssueStatus.OPEN)
            .HasColumnType("issue_status_enum");

        entity.Property(i => i.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(0);

        entity.Property(i => i.IssueType)
            .HasColumnName("issue_type")
            .IsRequired(false)
            .HasColumnType("issue_type_enum");

        entity.Property(i => i.ResolutionDetails).HasColumnName("resolution_details").IsRequired(false);
        entity.Property(i => i.ResolvedAt).HasColumnName("resolved_at").IsRequired(false);

        entity.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        entity.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(i => i.TaskId).HasColumnName("task_id").IsRequired(false);
        entity.Property(i => i.AssetId).HasColumnName("asset_id").IsRequired();
        entity.Property(i => i.AnnotationId).HasColumnName("annotation_id").IsRequired(false);
        entity.Property(i => i.ReportedByUserId).HasColumnName("reported_by_user_id").IsRequired();
        entity.Property(i => i.AssignedToUserId).HasColumnName("assigned_to_user_id").IsRequired(false);

        // Relationships
        entity.HasOne(i => i.Task)
            .WithMany(t => t.Issues)
            .HasForeignKey(i => i.TaskId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        entity.HasOne(i => i.Asset)
            .WithMany()
            .HasForeignKey(i => i.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(i => i.Annotation)
            .WithMany()
            .HasForeignKey(i => i.AnnotationId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        entity.HasOne(i => i.ReportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(i => i.AssignedToUser)
            .WithMany()
            .HasForeignKey(i => i.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
