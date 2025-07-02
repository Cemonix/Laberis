using server.Models.Common;
using server.Models.DTOs.Workflow;

namespace server.Services.Interfaces;

public interface IWorkflowService
{
    /// <summary>
    /// Retrieves all workflows for a specific project, optionally filtered and sorted.
    /// </summary>
    /// <param name="projectId">The ID of the project to retrieve workflows for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "name").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "name", "created_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of WorkflowDto.</returns>
    Task<PaginatedResponse<WorkflowDto>> GetWorkflowsForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a workflow by its ID.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the WorkflowDto if found, otherwise null.</returns>
    Task<WorkflowDto?> GetWorkflowByIdAsync(int workflowId);

    /// <summary>
    /// Creates a new workflow.
    /// </summary>
    /// <param name="projectId">The ID of the project to create the workflow in.</param>
    /// <param name="createDto">The DTO containing information for the new workflow.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created WorkflowDto.</returns>
    Task<WorkflowDto> CreateWorkflowAsync(int projectId, CreateWorkflowDto createDto);

    /// <summary>
    /// Updates an existing workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to update.</param>
    /// <param name="updateDto">The DTO containing updated workflow information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated WorkflowDto if successful, otherwise null.</returns>
    Task<WorkflowDto?> UpdateWorkflowAsync(int workflowId, UpdateWorkflowDto updateDto);

    /// <summary>
    /// Deletes a workflow by its ID.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the workflow was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteWorkflowAsync(int workflowId);
}
