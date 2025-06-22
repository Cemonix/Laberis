using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class Annotation
{
    public long AnnotationId { get; init; }
    public AnnotationType AnnotationType { get; init; }
    public string Data { get; init; } = string.Empty;
    public bool IsPrediction { get; init; }
    public double? ConfidenceScore { get; init; }
    public bool IsGroundTruth { get; init; }
    public int Version { get; init; }
    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    // Foreign Keys
    public int TaskId { get; init; }
    public int AssetId { get; init; }
    public int LabelId { get; init; }
    public string AnnotatorUserId { get; init; } = string.Empty;
    public long? ParentAnnotationId { get; init; }

    // Navigation Properties
    public virtual Task Task { get; init; } = null!;
    public virtual Asset Asset { get; init; } = null!;
    public virtual Label Label { get; init; } = null!;
    public virtual ApplicationUser AnnotatorUser { get; init; } = null!;
    public virtual Annotation? ParentAnnotation { get; init; }
    public virtual ICollection<Annotation> ChildAnnotations { get; init; } = [];
    public virtual ICollection<Issue> Issues { get; init; } = [];
}
