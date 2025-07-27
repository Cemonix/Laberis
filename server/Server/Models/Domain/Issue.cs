using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class Issue
{
    public int IssueId { get; set; }
    public string Description { get; set; } = string.Empty;
    public IssueStatus Status { get; set; }
    public int Priority { get; set; }
    public IssueType? IssueType { get; set; }
    public string? ResolutionDetails { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Foreign Keys
    public int? TaskId { get; set; }
    public int AssetId { get; set; }
    public long? AnnotationId { get; set; }
    public string ReportedByUserId { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }

    // Navigation Properties
    public virtual Task? Task { get; set; }
    public virtual Asset Asset { get; set; } = null!;
    public virtual Annotation? Annotation { get; set; }
    public virtual ApplicationUser ReportedByUser { get; set; } = null!;
    public virtual ApplicationUser? AssignedToUser { get; set; }
}
