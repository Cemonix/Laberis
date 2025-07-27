using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class Annotation
{
    public long AnnotationId { get; set; }
    public AnnotationType AnnotationType { get; set; }
    public string Data { get; set; } = string.Empty;
    public bool IsPrediction { get; set; }
    public double? ConfidenceScore { get; set; }
    public bool IsGroundTruth { get; set; }
    public int Version { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Foreign Keys
    public int TaskId { get; set; }
    public int AssetId { get; set; }
    public int LabelId { get; set; }
    public string AnnotatorUserId { get; set; } = string.Empty;
    public long? ParentAnnotationId { get; set; }

    // Navigation Properties
    public virtual Task Task { get; set; } = null!;
    public virtual Asset Asset { get; set; } = null!;
    public virtual Label Label { get; set; } = null!;
    public virtual ApplicationUser AnnotatorUser { get; set; } = null!;
    public virtual Annotation? ParentAnnotation { get; set; }
    public virtual ICollection<Annotation> ChildAnnotations { get; set; } = [];
    public virtual ICollection<Issue> Issues { get; set; } = [];
}
