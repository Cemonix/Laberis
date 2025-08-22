namespace server.Core.Alerts.Models;

/// <summary>
/// Represents a critical system alert that requires management intervention.
/// Created when pipeline rollback operations fail.
/// </summary>
public class ManagementAlert
{
    public int AlertId { get; set; }
    
    /// <summary>
    /// Type of alert that was raised.
    /// </summary>
    public AlertType Type { get; set; }
    
    /// <summary>
    /// The task ID involved in the failed operation.
    /// </summary>
    public int TaskId { get; set; }
    
    /// <summary>
    /// The asset ID involved in the failed operation.
    /// </summary>
    public int AssetId { get; set; }
    
    /// <summary>
    /// Description of why the pipeline operation failed.
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed error message from the original operation.
    /// </summary>
    public string OriginalError { get; set; } = string.Empty;
    
    /// <summary>
    /// Details about the rollback failure.
    /// </summary>
    public string? RollbackError { get; set; }
    
    /// <summary>
    /// User ID who initiated the failed operation.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this alert has been resolved by management.
    /// </summary>
    public bool IsResolved { get; set; }
    
    /// <summary>
    /// Notes added by management when resolving the alert.
    /// </summary>
    public string? ResolutionNotes { get; set; }
    
    /// <summary>
    /// User ID of the manager who resolved the alert.
    /// </summary>
    public string? ResolvedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Types of management alerts that can be raised.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Pipeline execution failed and rollback also failed.
    /// </summary>
    PIPELINE_ROLLBACK_FAILED,
    
    /// <summary>
    /// Asset transfer operation failed during pipeline execution.
    /// </summary>
    ASSET_TRANSFER_FAILED,
    
    /// <summary>
    /// Task status update failed during pipeline execution.
    /// </summary>
    TASK_STATUS_UPDATE_FAILED,
    
    /// <summary>
    /// Data integrity issue detected during pipeline execution.
    /// </summary>
    DATA_INTEGRITY_VIOLATION
}