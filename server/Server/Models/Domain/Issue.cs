using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class Issue
{
    public int IssueId { get; init; }
    public string Description { get; init; } = string.Empty;
    public IssueStatus Status { get; init; }
    public int Priority { get; init; }
    public IssueType? IssueType { get; init; }
    public string? ResolutionDetails { get; init; }
    public DateTime? ResolvedAt { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    // Foreign Keys
    public int? TaskId { get; init; }
    public int AssetId { get; init; }
    public long? AnnotationId { get; init; }
    public string ReportedByUserId { get; init; } = string.Empty;
    public string? AssignedToUserId { get; init; }

    // Navigation Properties
    public virtual Task? Task { get; init; }
    public virtual Asset Asset { get; init; } = null!;
    public virtual Annotation? Annotation { get; init; }
    public virtual IdentityUser ReportedByUser { get; init; } = null!;
    public virtual IdentityUser? AssignedToUser { get; init; }
}
