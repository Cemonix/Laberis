namespace server.Models.Domain;

/// <summary>
/// Represents a connection between two workflow stages in a pipeline.
/// This enables many-to-many relationships where multiple stages can lead to one stage.
/// </summary>
public class WorkflowStageConnection
{
    public int WorkflowStageConnectionId { get; set; }
    
    // The stage that comes before in the pipeline
    public int FromStageId { get; set; }
    public virtual WorkflowStage FromStage { get; set; } = null!;
    
    // The stage that comes after in the pipeline
    public int ToStageId { get; set; }
    public virtual WorkflowStage ToStage { get; set; } = null!;
    
    // Optional: Condition for this connection (e.g., "approved", "rejected")
    public string? Condition { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
