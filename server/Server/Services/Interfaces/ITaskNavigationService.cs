using server.Models.DTOs.Task;

namespace server.Services.Interfaces;

/// <summary>
/// Service for handling task navigation operations
/// </summary>
public interface ITaskNavigationService
{
    /// <summary>
    /// Gets the next available task for the specified user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Task navigation information</returns>
    Task<TaskNavigationDto> GetNextTaskAsync(string userId, int currentTaskId, int? workflowStageId = null);

    /// <summary>
    /// Gets the previous available task for the specified user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Task navigation information</returns>
    Task<TaskNavigationDto> GetPreviousTaskAsync(string userId, int currentTaskId, int? workflowStageId = null);

    /// <summary>
    /// Gets the navigation context for the current task
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="currentTaskId">The ID of the current task</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by</param>
    /// <returns>Task navigation context information</returns>
    Task<TaskNavigationDto> GetNavigationContextAsync(string userId, int currentTaskId, int? workflowStageId = null);
}