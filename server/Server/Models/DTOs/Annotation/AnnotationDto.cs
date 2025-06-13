using server.Models.Domain.Enums;

namespace server.Models.DTOs.Annotation;

public record class AnnotationDto
{
    public long Id { get; init; }
    public AnnotationType AnnotationType { get; init; }
    public string Data { get; init; } = string.Empty;
    public bool IsPrediction { get; init; }
    public double? ConfidenceScore { get; init; }
    public bool IsGroundTruth { get; init; }
    public int Version { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int TaskId { get; init; }
    public int AssetId { get; init; }
    public int LabelId { get; init; }
    public string AnnotatorUserId { get; init; } = string.Empty;
    public long? ParentAnnotationId { get; init; }
}
