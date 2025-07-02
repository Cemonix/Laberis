using server.Models.Domain.Enums;

namespace server.Models.DTOs.WorkflowStage;

public record class WorkflowStageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int StageOrder { get; init; }
    public WorkflowStageType? StageType { get; init; }
    public bool IsInitialStage { get; init; }
    public bool IsFinalStage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int WorkflowId { get; init; }
    public int? InputDataSourceId { get; init; }
    public int? TargetDataSourceId { get; init; }
    
    // Pipeline relationships
    public ICollection<WorkflowStageConnectionDto> IncomingConnections { get; init; } = [];
    public ICollection<WorkflowStageConnectionDto> OutgoingConnections { get; init; } = [];
    
    // User assignments
    public ICollection<WorkflowStageAssignmentDto> Assignments { get; init; } = [];
}
