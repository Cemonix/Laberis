using server.Models.Domain.Enums;

namespace server.Models.DTOs.Task;

public record class TaskDto
{
    public int Id { get; init; }
    public int Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime? SuspendedAt { get; init; }
    public DateTime? DeferredAt { get; init; }
    public server.Models.Domain.Enums.TaskStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int AssetId { get; init; }
    public int ProjectId { get; init; }
    public int WorkflowId { get; init; }
    public int CurrentWorkflowStageId { get; init; }
    public string? AssignedToEmail { get; init; }
    public string? LastWorkedOnByEmail { get; init; }
}
