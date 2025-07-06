using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Workflow;

public record class CreateWorkflowStageWithAssignmentsDto
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Required]
    public int StageOrder { get; init; }

    public WorkflowStageType? StageType { get; init; }

    public bool IsInitialStage { get; init; } = false;

    public bool IsFinalStage { get; init; } = false;

    public int? InputDataSourceId { get; init; }

    public int? TargetDataSourceId { get; init; }

    /// <summary>
    /// List of project member IDs to assign to this workflow stage
    /// </summary>
    public ICollection<int> AssignedProjectMemberIds { get; init; } = [];
}
