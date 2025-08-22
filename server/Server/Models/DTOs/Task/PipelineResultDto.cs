namespace server.Models.DTOs.Task;

/// <summary>
/// DTO for pipeline operation results
/// </summary>
public class PipelineResultDto
{
    /// <summary>
    /// Indicates if the pipeline operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The updated task after the pipeline operation
    /// </summary>
    public TaskDto? UpdatedTask { get; set; }

    /// <summary>
    /// Additional context about the pipeline execution
    /// </summary>
    public string? Details { get; set; }
}