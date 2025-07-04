using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.WorkflowStage;

public record class UpdateWorkflowStageConnectionDto
{
    [Required]
    public int FromStageId { get; init; }

    [Required]
    public int ToStageId { get; init; }

    public string? Condition { get; init; }
}
