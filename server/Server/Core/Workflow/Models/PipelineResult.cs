using LaberisTask = server.Models.Domain.Task;

namespace server.Core.Workflow.Models;

/// <summary>
/// Result of a pipeline execution containing success status and relevant data.
/// </summary>
public class PipelineResult
{
    private PipelineResult(
        bool isSuccess,
        LaberisTask? updatedTask = null,
        LaberisTask? createdTask = null,
        string? errorMessage = null,
        Exception? exception = null,
        string? details = null)
    {
        IsSuccess = isSuccess;
        UpdatedTask = updatedTask;
        CreatedTask = createdTask;
        ErrorMessage = errorMessage;
        Exception = exception;
        Details = details;
    }

    /// <summary>
    /// Whether the pipeline execution was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The task that was updated during pipeline execution.
    /// </summary>
    public LaberisTask? UpdatedTask { get; }

    /// <summary>
    /// New task created during pipeline execution (for forward flow).
    /// </summary>
    public LaberisTask? CreatedTask { get; }

    /// <summary>
    /// Error message if the pipeline failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Exception that caused the pipeline to fail.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Additional details about the pipeline execution.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Creates a successful pipeline result.
    /// </summary>
    public static PipelineResult Success(LaberisTask updatedTask, LaberisTask? createdTask = null, string? details = null)
    {
        return new PipelineResult(true, updatedTask, createdTask, details: details);
    }

    /// <summary>
    /// Creates a failed pipeline result with error message.
    /// </summary>
    public static PipelineResult Failure(string errorMessage, Exception? exception = null)
    {
        return new PipelineResult(false, errorMessage: errorMessage, exception: exception);
    }
}