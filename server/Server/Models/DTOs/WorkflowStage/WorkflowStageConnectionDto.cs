namespace server.Models.DTOs.WorkflowStage;

public record class WorkflowStageConnectionDto
{
    public int Id { get; init; }
    public int FromStageId { get; init; }
    public int ToStageId { get; init; }
    public string? Condition { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
