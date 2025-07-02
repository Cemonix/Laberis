namespace server.Models.Domain;

/// <summary>
/// Represents a connection between two workflow stages in a pipeline.
/// This enables many-to-many relationships where multiple stages can lead to one stage.
/// </summary>
public record class WorkflowStageConnection
{
    public int WorkflowStageConnectionId { get; init; }
    
    // The stage that comes before in the pipeline
    public int FromStageId { get; init; }
    public virtual WorkflowStage FromStage { get; init; } = null!;
    
    // The stage that comes after in the pipeline
    public int ToStageId { get; init; }
    public virtual WorkflowStage ToStage { get; init; } = null!;
    
    // Optional: Condition for this connection (e.g., "approved", "rejected")
    public string? Condition { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
