using server.Models.Common;
using server.Models.DTOs.WorkflowStage;
using server.Models.DTOs.Workflow;

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

    /// <summary>
    /// Gets workflow stages with their connections populated for pipeline visualization.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to get stages for.</param>
    /// <returns>A task that represents the asynchronous operation, containing workflow stages with connection information.</returns>
    Task<IEnumerable<WorkflowStageDto>> GetWorkflowStagesWithConnectionsAsync(int workflowId);

    /// <summary>
    /// Validates if a workflow stage belongs to the specified workflow.
    /// </summary>
    /// <param name="stageId">The ID of the workflow stage.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the stage belongs to the workflow, otherwise false.</returns>
    Task<bool> ValidateStageBelongsToWorkflowAsync(int stageId, int workflowId);
    
    /// <summary>
    /// Gets workflow stages that are using a specific data source.
    /// Used to prevent data source conflicts when creating new workflows.
    /// Enforces 1:1 mapping between data sources and workflow stages for data integrity.
    /// </summary>
    /// <param name="dataSourceId">The ID of the data source to check.</param>
    /// <param name="excludeWorkflowId">Optional workflow ID to exclude from the check (for updates).</param>
    /// <returns>A task that represents the asynchronous operation, containing workflow stages using the data source.</returns>
    Task<IEnumerable<WorkflowStageDto>> GetDataSourceUsageConflictsAsync(int dataSourceId, int? excludeWorkflowId = null);

    /// <summary>
    /// Creates a complete pipeline of default workflow stages for a workflow.
    /// Orchestrates the creation of default stages (annotation, review, completion) and
    /// manages data source requirements, returning the initial stage ID for task creation.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to create stages for.</param>
    /// <param name="projectId">The ID of the project (needed for data source management).</param>
    /// <param name="createDefaultStages">Whether to create default pipeline stages.</param>
    /// <param name="includeReviewStage">Whether to include a review stage in the default pipeline.</param>
    /// <returns>A task containing the initial stage ID and list of created stages.</returns>
    Task<(int? initialStageId, List<WorkflowStageDto> createdStages)> CreateWorkflowStagesPipelineAsync(
        int workflowId, 
        int projectId, 
        bool createDefaultStages, 
        bool includeReviewStage);
}
