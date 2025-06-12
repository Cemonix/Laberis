using server.Models.Domain.Enums;

namespace server.Models.DTOs.Project;

public record class ProjectDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? AnnotationGuidelinesUrl { get; init; }
    public string? OwnerId { get; init; }
    public ProjectType ProjectType { get; init; }
    public ProjectStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
