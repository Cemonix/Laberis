using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.ProjectDto;

public record class UpdateProjectDto
{
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Project name must be between 3 and 100 characters.")]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Required(ErrorMessage = "Project status is required.")]
    [EnumDataType(typeof(ProjectStatus), ErrorMessage = "Invalid project status specified.")]
    public ProjectStatus Status { get; init; }

    public string? AnnotationGuidelinesUrl { get; init; }
}