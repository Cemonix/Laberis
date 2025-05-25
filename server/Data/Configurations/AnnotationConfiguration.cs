using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;

namespace server.Data.Configurations;

public class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> entity)
    {
        entity.ToTable("annotations");

        entity.HasKey(ann => ann.AnnotationId);
        entity.Property(ann => ann.AnnotationId).HasColumnName("annotation_id").ValueGeneratedOnAdd();

        entity.Property(ann => ann.AnnotationType)
            .HasColumnName("annotation_type")
            .IsRequired()
            .HasColumnType("annotation_type_enum");

        entity.Property(ann => ann.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb") // Assuming TEXT in DB for JSON string
            .IsRequired();

        entity.Property(ann => ann.IsPrediction).HasColumnName("is_prediction").IsRequired().HasDefaultValue(false);
        entity.Property(ann => ann.ConfidenceScore).HasColumnName("confidence_score").IsRequired(false);
        entity.Property(ann => ann.IsGroundTruth).HasColumnName("is_ground_truth").IsRequired().HasDefaultValue(false);
        entity.Property(ann => ann.Version).HasColumnName("version").IsRequired().HasDefaultValue(1);
        entity.Property(ann => ann.Notes).HasColumnName("notes").IsRequired(false);

        entity.Property(ann => ann.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        entity.Property(ann => ann.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(ann => ann.TaskId).HasColumnName("task_id");
        entity.Property(ann => ann.AssetId).HasColumnName("asset_id");
        entity.Property(ann => ann.LabelId).HasColumnName("label_id");
        entity.Property(ann => ann.AnnotatorUserId).HasColumnName("annotator_user_id").IsRequired();
        entity.Property(ann => ann.ParentAnnotationId).HasColumnName("parent_annotation_id").IsRequired(false);

        // Relationships
        entity.HasOne(ann => ann.Task)
            .WithMany(t => t.Annotations)
            .HasForeignKey(ann => ann.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(ann => ann.Asset)
            .WithMany(a => a.Annotations)
            .HasForeignKey(ann => ann.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(ann => ann.Label)
            .WithMany()
            .HasForeignKey(ann => ann.LabelId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(ann => ann.AnnotatorUser)
            .WithMany()
            .HasForeignKey(ann => ann.AnnotatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(ann => ann.ParentAnnotation)
            .WithMany(pa => pa.ChildAnnotations)
            .HasForeignKey(ann => ann.ParentAnnotationId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Annotation to Issues (One-to-Many)
        entity.HasMany(ann => ann.Issues)
            .WithOne(i => i.Annotation)
            .HasForeignKey(i => i.AnnotationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
