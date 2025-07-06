using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Workflow;

public record class CreateWorkflowDto
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>
    /// Workflow stages to create along with the workflow.
    /// If empty, no stages will be created automatically.
    /// </summary>
    public ICollection<CreateWorkflowStageWithAssignmentsDto> Stages { get; init; } = [];

    /// <summary>
    /// Whether to create default stages (annotation and completion) if no stages are provided.
    /// Default stages will be created automatically with proper order and stage types.
    /// </summary>
    public bool CreateDefaultStages { get; init; } = false;

    /// <summary>
    /// Whether to include a review stage between annotation and completion stages.
    /// Only applies when CreateDefaultStages is true.
    /// </summary>
    public bool IncludeReviewStage { get; init; } = false;
}
