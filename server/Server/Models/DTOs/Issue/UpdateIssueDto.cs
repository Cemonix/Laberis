using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Issue;

public record class UpdateIssueDto
{
    [StringLength(1000, MinimumLength = 10)]
    public string? Description { get; init; }

    public IssueStatus? Status { get; init; }

    [Range(1, 5)]
    public int? Priority { get; init; }

    public IssueType? IssueType { get; init; }

    public string? ResolutionDetails { get; init; }

    public string? AssignedToUserId { get; init; }
}
