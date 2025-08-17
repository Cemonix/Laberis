namespace server.Models.Internal;

/// <summary>
/// Result information for asset movement operations during workflow progression.
/// </summary>
public class AssetMovementResult
{
    /// <summary>
    /// Whether the asset was moved to a different data source.
    /// </summary>
    public bool AssetMoved { get; set; }

    /// <summary>
    /// Whether the current task should be archived.
    /// </summary>
    public bool ShouldArchiveTask { get; set; }

    /// <summary>
    /// The ID of the workflow stage where tasks should be created, if any.
    /// TaskService will handle the actual task creation.
    /// </summary>
    public int? TargetWorkflowStageId { get; set; }

    /// <summary>
    /// The data source ID where tasks should be created, if any.
    /// TaskService will use this information to create tasks.
    /// </summary>
    public int? TargetDataSourceId { get; set; }

    /// <summary>
    /// Any error messages that occurred during the process.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
