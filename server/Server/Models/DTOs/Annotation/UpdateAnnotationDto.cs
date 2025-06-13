using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Annotation;

public record class UpdateAnnotationDto
{
    public string? Data { get; init; }

    public int? LabelId { get; init; }

    public bool? IsPrediction { get; init; }

    [Range(0.0, 1.0)]
    public double? ConfidenceScore { get; init; }

    public bool? IsGroundTruth { get; init; }

    public string? Notes { get; init; }
}
