using LaberisTask = server.Models.Domain.Task;
using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<LaberisTask>
{
    /// <summary>
    /// Finds a task by asset ID and workflow stage ID.
    /// Used during veto operations to locate annotation tasks for updates.
    /// </summary>
    /// <param name="assetId">The ID of the asset.</param>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <returns>The task if found, null otherwise.</returns>
    Task<LaberisTask?> FindByAssetAndStageAsync(int assetId, int workflowStageId);

    /// <summary>
    /// Gets all tasks for a specific asset across all workflow stages.
    /// Useful for data integrity validation and workflow tracking.
    /// </summary>
    /// <param name="assetId">The ID of the asset.</param>
    /// <returns>All tasks associated with the asset.</returns>
    Task<IEnumerable<LaberisTask>> GetTasksByAssetIdAsync(int assetId);

    /// <summary>
    /// Updates a task's status and related timestamps in a single operation.
    /// Used by pipeline steps for atomic status updates.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="newStatus">The new status to set.</param>
    /// <param name="userId">The ID of the user making the change.</param>
    /// <returns>The updated task.</returns>
    Task<LaberisTask> UpdateTaskStatusAsync(LaberisTask task, Models.Domain.Enums.TaskStatus newStatus, string userId);

    /// <summary>
    /// Gets navigable tasks for a user in a project, ordered by creation date and task ID.
    /// Includes tasks assigned to the user and unassigned tasks (available for assignment).
    /// Excludes completed, archived, and vetoed tasks.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowStageId">Optional workflow stage ID to filter by.</param>
    /// <returns>List of navigable tasks ordered by CreatedAt then TaskId.</returns>
    Task<List<LaberisTask>> GetNavigableTasksAsync(string userId, int projectId, int? workflowStageId = null);
}
