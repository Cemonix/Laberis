using server.Models.Domain.Enums;

namespace server.Models.DTOs.WorkflowStage;

/// <summary>
/// DTO specifically designed for pipeline visualization with simplified data
/// </summary>
public record class WorkflowStagePipelineDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int StageOrder { get; init; }
    public WorkflowStageType? StageType { get; init; }
    public bool IsInitialStage { get; init; }
    public bool IsFinalStage { get; init; }
    
    // Simplified connections for visualization
    public ICollection<int> PreviousStageIds { get; init; } = [];
    public ICollection<int> NextStageIds { get; init; } = [];
    
    // User count for quick overview
    public int AssignedUserCount { get; init; }
    
    // Position for canvas (can be set by frontend)
    public double? PositionX { get; init; }
    public double? PositionY { get; init; }
}
