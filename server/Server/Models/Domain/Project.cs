using System;
using Microsoft.AspNetCore.Identity;
using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class Project
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectType ProjectType { get; set; } = ProjectType.OTHER;
    public ProjectStatus Status { get; set; } = ProjectStatus.ACTIVE;

    public string? OwnerId { get; set; }
    public string? AnnotationGuidelinesUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ApplicationUser? Owner { get; set; }

    public virtual ICollection<LabelScheme> LabelSchemes { get; set; } = [];
    public virtual ICollection<DataSource> DataSources { get; set; } = [];
    public virtual ICollection<Asset> Assets { get; set; } = [];
    public virtual ICollection<Workflow> Workflows { get; set; } = [];
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = [];
}
