namespace server.Models.DTOs.Task;

public record class UpdateTaskDto
{
    public int? Priority { get; init; }

    public DateTime? DueDate { get; init; }

    public string? Metadata { get; init; }

    public int? CurrentWorkflowStageId { get; init; }

    public string? AssignedToUserId { get; init; }

    public string? AssignedToEmail { get; init; }

    public Domain.Enums.TaskStatus? Status { get; init; }

    public DateTime? CompletedAt { get; init; }

    public DateTime? ArchivedAt { get; init; }

    public DateTime? SuspendedAt { get; init; }

    public DateTime? DeferredAt { get; init; }

    public long? WorkingTimeMs { get; init; }
}
