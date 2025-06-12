using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Project;

public record class CreateProjectDto
{
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Project name must be between 3 and 100 characters.")]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Required(ErrorMessage = "Project type is required.")]
    [EnumDataType(typeof(ProjectType), ErrorMessage = "Invalid project type specified.")]
    public ProjectType ProjectType { get; init; }
}
