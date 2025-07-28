using server.Models.Common;
using server.Models.DTOs.Task;

namespace server.Services.Interfaces;

public interface ITaskService
{
    /// <summary>
    /// Retrieves all tasks for a specific project, optionally filtered and sorted.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve tasks for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "priority", "assigned_to_user_id", "current_workflow_stage_id", "is_completed").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "priority", "due_date", "created_at", "completed_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of TaskDto.</returns>
    Task<PaginatedResponse<TaskDto>> GetTasksForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves all tasks assigned to a specific user, optionally filtered and sorted.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve assigned tasks for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "priority", "project_id", "current_workflow_stage_id", "is_completed").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "priority", "due_date", "created_at", "completed_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of TaskDto.</returns>
    Task<PaginatedResponse<TaskDto>> GetTasksForUserAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a task by its ID.
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the TaskDto if found, otherwise null.</returns>
    Task<TaskDto?> GetTaskByIdAsync(int taskId);

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the task for.</param>
    /// <param name="createDto">The DTO containing information for the new task.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created TaskDto.</returns>
    Task<TaskDto> CreateTaskAsync(int projectId, CreateTaskDto createDto);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="taskId">The ID of the task to update.</param>
    /// <param name="updateDto">The DTO containing updated task information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> UpdateTaskAsync(int taskId, UpdateTaskDto updateDto);

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="taskId">The ID of the task to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the task was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteTaskAsync(int taskId);

    /// <summary>
    /// Assigns a task to a user.
    /// </summary>
    /// <param name="taskId">The ID of the task to assign.</param>
    /// <param name="userId">The ID of the user to assign the task to.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> AssignTaskAsync(int taskId, string userId);

    /// <summary>
    /// Moves a task to a different workflow stage.
    /// </summary>
    /// <param name="taskId">The ID of the task to move.</param>
    /// <param name="workflowStageId">The ID of the workflow stage to move the task to.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> MoveTaskToStageAsync(int taskId, int workflowStageId, string userId);

    /// <summary>
    /// Creates tasks automatically for all assets in a project when a workflow is created.
    /// This creates tasks at the initial workflow stage for all imported assets.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="initialStageId">The ID of the initial workflow stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing the number of tasks created.</returns>
    Task<int> CreateTasksForAllAssetsAsync(int projectId, int workflowId, int initialStageId);

    /// <summary>
    /// Creates tasks for all available assets in a specific data source for a workflow stage.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <param name="dataSourceId">The ID of the data source.</param>
    /// <returns>A task that represents the asynchronous operation, containing the number of tasks created.</returns>
    Task<int> CreateTasksForDataSourceAsync(int projectId, int workflowId, int workflowStageId, int dataSourceId);

    /// <summary>
    /// Checks if a data source has any assets available for task creation.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="dataSourceId">The ID of the data source to check.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if assets are available, otherwise false.</returns>
    Task<bool> HasAssetsAvailableAsync(int projectId, int dataSourceId);

    /// <summary>
    /// Gets the count of assets available in a project for task creation.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation, containing the count of available assets.</returns>
    Task<int> GetAvailableAssetsCountAsync(int projectId);

    /// <summary>
    /// Marks a task as completed, unlocking the asset for subsequent workflow stages.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="userId">The ID of the user completing the task.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> CompleteTaskAsync(int taskId, string userId);

    /// <summary>
    /// Marks a task as completed and moves it to the next workflow stage if one exists.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="userId">The ID of the user completing the task.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> CompleteAndMoveTaskAsync(int taskId, string userId);

    /// <summary>
    /// Marks a completed task as incomplete, allowing it to be worked on again.
    /// </summary>
    /// <param name="taskId">The ID of the task to mark as incomplete.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> MarkTaskIncompleteAsync(int taskId, string userId);

    /// <summary>
    /// Suspends a task, marking it as suspended so it can be resumed later.
    /// </summary>
    /// <param name="taskId">The ID of the task to suspend.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> SuspendTaskAsync(int taskId, string userId);

    /// <summary>
    /// Unsuspends a task by clearing its suspended status.
    /// </summary>
    /// <param name="taskId">The ID of the task to unsuspend.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated TaskDto if successful, otherwise null.</returns>
    Task<TaskDto?> UnsuspendTaskAsync(int taskId, string userId);
}
