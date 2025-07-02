using server.Models.Domain.Enums;

namespace server.Models.DTOs.Issue;

public record class IssueDto
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public IssueStatus Status { get; init; }
    public int Priority { get; init; }
    public IssueType? IssueType { get; init; }
    public string? ResolutionDetails { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int? TaskId { get; init; }
    public int AssetId { get; init; }
    public long? AnnotationId { get; init; }
    public string ReportedByEmail { get; init; } = string.Empty;
    public string? AssignedToEmail { get; init; }
}
