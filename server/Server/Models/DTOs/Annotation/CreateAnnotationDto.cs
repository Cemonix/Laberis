using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Annotation;

public record class CreateAnnotationDto
{
    [Required]
    public AnnotationType AnnotationType { get; init; }

    [Required]
    public string Data { get; init; } = string.Empty;

    [Required]
    public int TaskId { get; init; }

    [Required]
    public int AssetId { get; init; }

    [Required]
    public int LabelId { get; init; }

    public bool IsPrediction { get; init; } = false;

    [Range(0.0, 1.0)]
    public double? ConfidenceScore { get; init; }

    public bool IsGroundTruth { get; init; } = false;

    public string? Notes { get; init; }

    public long? ParentAnnotationId { get; init; }
}
