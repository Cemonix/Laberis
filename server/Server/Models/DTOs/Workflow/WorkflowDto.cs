namespace server.Models.DTOs.Workflow;

public record class WorkflowDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ProjectId { get; init; }
}
