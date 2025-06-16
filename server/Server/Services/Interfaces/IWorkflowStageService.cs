using server.Models.Common;
using server.Models.DTOs.WorkflowStage;

namespace server.Services.Interfaces;

public interface IWorkflowStageService
{
    /// <summary>
    /// Retrieves all workflow stages for a specific workflow, optionally filtered and sorted.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to retrieve stages for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "name", "stage_type", "is_initial_stage", "is_final_stage").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "stage_order", "name", "created_at").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of WorkflowStageDto.</returns>
    Task<PaginatedResponse<WorkflowStageDto>> GetWorkflowStagesAsync(
        int workflowId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves a workflow stage by its ID.
    /// </summary>
    /// <param name="stageId">The ID of the workflow stage to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the WorkflowStageDto if found, otherwise null.</returns>
    Task<WorkflowStageDto?> GetWorkflowStageByIdAsync(int stageId);

    /// <summary>
    /// Creates a new workflow stage.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to create the stage in.</param>
    /// <param name="createDto">The DTO containing information for the new workflow stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created WorkflowStageDto.</returns>
    Task<WorkflowStageDto> CreateWorkflowStageAsync(int workflowId, CreateWorkflowStageDto createDto);

    /// <summary>
    /// Updates an existing workflow stage.
    /// </summary>
    /// <param name="stageId">The ID of the workflow stage to update.</param>
    /// <param name="updateDto">The DTO containing updated workflow stage information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated WorkflowStageDto if successful, otherwise null.</returns>
    Task<WorkflowStageDto?> UpdateWorkflowStageAsync(int stageId, UpdateWorkflowStageDto updateDto);

    /// <summary>
    /// Deletes a workflow stage by its ID.
    /// </summary>
    /// <param name="stageId">The ID of the workflow stage to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the workflow stage was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteWorkflowStageAsync(int stageId);

    /// <summary>
    /// Reorders workflow stages within a workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to reorder stages for.</param>
    /// <param name="stageOrderMap">A dictionary mapping stage IDs to their new order positions.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the reordering was successful, otherwise false.</returns>
    Task<bool> ReorderWorkflowStagesAsync(int workflowId, Dictionary<int, int> stageOrderMap);
}
