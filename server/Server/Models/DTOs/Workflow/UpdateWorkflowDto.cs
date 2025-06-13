using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Workflow;

public record class UpdateWorkflowDto
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsDefault { get; init; }
}
