using server.Models.Domain;
using server.Models.Domain.Enums;
using LaberisTask = server.Models.Domain.Task;

namespace server.Core.Workflow.Models;

/// <summary>
/// Context object that carries data through the pipeline execution steps.
/// Immutable to ensure thread safety and prevent side effects.
/// </summary>
public class PipelineContext
{
    public PipelineContext(
        LaberisTask task,
        Asset asset,
        WorkflowStage currentStage,
        string userId,
        string? reason = null)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        Asset = asset ?? throw new ArgumentNullException(nameof(asset));
        CurrentStage = currentStage ?? throw new ArgumentNullException(nameof(currentStage));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The task being processed through the pipeline.
    /// </summary>
    public LaberisTask Task { get; }

    /// <summary>
    /// The asset associated with the task.
    /// </summary>
    public Asset Asset { get; }

    /// <summary>
    /// The current workflow stage where the task is located.
    /// </summary>
    public WorkflowStage CurrentStage { get; }

    /// <summary>
    /// The ID of the user who initiated the pipeline action.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Optional reason for the action (e.g., veto reason).
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// When the pipeline context was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Target stage for asset movement (set during pipeline execution).
    /// </summary>
    public WorkflowStage? TargetStage { get; private set; }

    /// <summary>
    /// Step execution context for tracking rollback state per step.
    /// Key: Step name, Value: Step-specific rollback data
    /// </summary>
    public Dictionary<string, object> StepContext { get; private set; } = new();

    /// <summary>
    /// Creates a new context with the target stage set.
    /// </summary>
    public PipelineContext WithTargetStage(WorkflowStage targetStage)
    {
        // Create a new context to keep the original context unchanged - immutability due to threading
        var newContext = new PipelineContext(Task, Asset, CurrentStage, UserId, Reason)
        {
            TargetStage = targetStage ?? throw new ArgumentNullException(nameof(targetStage)),
            StepContext = new Dictionary<string, object>(StepContext)
        };
        return newContext;
    }

    /// <summary>
    /// Sets step-specific context data for rollback purposes.
    /// </summary>
    public void SetStepContext<T>(string stepName, T data) where T : class
    {
        StepContext[stepName] = data;
    }

    /// <summary>
    /// Gets step-specific context data for rollback purposes.
    /// </summary>
    public T? GetStepContext<T>(string stepName) where T : class
    {
        return StepContext.TryGetValue(stepName, out var data) ? data as T : null;
    }
}