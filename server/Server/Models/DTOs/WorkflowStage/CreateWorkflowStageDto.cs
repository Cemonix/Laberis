using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.WorkflowStage;

public record class CreateWorkflowStageDto
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

    public string? UiConfiguration { get; init; }

    public int? InputDataSourceId { get; init; }

    public int? TargetDataSourceId { get; init; }
}
