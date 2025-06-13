using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Issue;

public record class CreateIssueDto
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public int AssetId { get; init; }

    [Required]
    [Range(1, 5)]
    public int Priority { get; init; } = 3;

    public IssueType? IssueType { get; init; }

    public int? TaskId { get; init; }

    public long? AnnotationId { get; init; }

    public string? AssignedToUserId { get; init; }
}
