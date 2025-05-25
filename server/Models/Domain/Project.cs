using System;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class Project
{
    public int ProjectId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public ProjectType ProjectType { get; init; }
    public ProjectStatus Status { get; init; }

    public string? OwnerId { get; init; }
    public string? AnnotationGuidelinesUrl { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public virtual ApplicationUser? Owner { get; init; }

    public virtual ICollection<LabelScheme> LabelSchemes { get; init; } = [];
    public virtual ICollection<DataSource> DataSources { get; init; } = [];
    public virtual ICollection<Asset> Assets { get; init; } = [];
    public virtual ICollection<Workflow> Workflows { get; init; } = [];
    public virtual ICollection<ProjectMember> ProjectMembers { get; init; } = [];
}
