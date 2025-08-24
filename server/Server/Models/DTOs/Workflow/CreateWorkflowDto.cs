using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Workflow;

public record class CreateWorkflowDto
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>
    /// The ID of the label scheme to assign to this workflow.
    /// All annotations in this workflow will use this label scheme.
    /// </summary>
    [Required]
    public int LabelSchemeId { get; init; }

    /// <summary>
    /// Workflow stages to create along with the workflow.
    /// </summary>
    public ICollection<CreateWorkflowStageWithAssignmentsDto> Stages { get; init; } = [];

    /// <summary>
    /// Whether to include a review stage between annotation and completion stages.
    /// If true: Annotation → Review → Completion
    /// If false: Annotation → Completion
    /// </summary>
    public bool IncludeReviewStage { get; init; } = false;
}
