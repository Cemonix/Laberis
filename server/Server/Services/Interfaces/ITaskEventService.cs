using server.Models.Common;
using server.Models.DTOs.TaskEvent;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Services.Interfaces;

public interface ITaskEventService
{
    /// <summary>
    /// Retrieves all events for a specific task, optionally filtered and sorted.
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve events for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "event_type", "user_id").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "event_type").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of TaskEventDto.</returns>
    Task<PaginatedResponse<TaskEventDto>> GetTaskEventsAsync(
        int taskId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a task event by its ID.
    /// </summary>
    /// <param name="eventId">The ID of the task event to retrieve.</param>
    /// <returns>Gets a task event by its ID.</returns>
    Task<TaskEventDto?> GetTaskEventByIdAsync(long eventId);

    /// <summary>
    /// Creates a new task event.
    /// </summary>
    /// <param name="createDto">The DTO containing information for the new task event.</param>
    /// <param name="userId">The ID of the user performing the action (optional).</param>
    /// <returns>Creates a new task event.</returns>
    Task<TaskEventDto> CreateTaskEventAsync(CreateTaskEventDto createDto, string? userId = null);

    /// <summary>
    /// Creates a task event specifically for status changes, with appropriate event type mapping.
    /// </summary>
    /// <param name="taskId">The ID of the task whose status changed.</param>
    /// <param name="fromStatus">The previous status of the task.</param>
    /// <param name="toStatus">The new status of the task.</param>
    /// <param name="userId">The ID of the user who performed the status change.</param>
    /// <returns>Logs a status change event.</returns>
    Task<TaskEventDto> CreateStatusChangeEventAsync(int taskId, TaskStatus fromStatus, TaskStatus toStatus, string userId);
}
