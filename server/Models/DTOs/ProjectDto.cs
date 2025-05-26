using server.Models.Domain.Enums;

namespace server.Models.DTOs;

public record class ProjectDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? OwnerId { get; init; }
    public ProjectType ProjectType { get; init; }
    public ProjectStatus Status { get; init; }
}
