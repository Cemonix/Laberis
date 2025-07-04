using server.Models.DTOs.WorkflowStage;

namespace server.Services.Interfaces;

public interface IWorkflowStageConnectionService
{
    /// <summary>
    /// Gets all connections for a specific workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, containing all connections for the workflow.</returns>
    Task<IEnumerable<WorkflowStageConnectionDto>> GetConnectionsForWorkflowAsync(int workflowId);

    /// <summary>
    /// Gets a specific connection by its ID.
    /// </summary>
    /// <param name="connectionId">The ID of the connection.</param>
    /// <returns>A task that represents the asynchronous operation, containing the connection if found, otherwise null.</returns>
    Task<WorkflowStageConnectionDto?> GetConnectionByIdAsync(int connectionId);

    /// <summary>
    /// Creates a new connection between two workflow stages.
    /// </summary>
    /// <param name="createDto">The connection creation data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created connection.</returns>
    Task<WorkflowStageConnectionDto> CreateConnectionAsync(CreateWorkflowStageConnectionDto createDto);

    /// <summary>
    /// Updates an existing connection.
    /// </summary>
    /// <param name="connectionId">The ID of the connection to update.</param>
    /// <param name="updateDto">The connection update data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated connection if successful, otherwise null.</returns>
    Task<WorkflowStageConnectionDto?> UpdateConnectionAsync(int connectionId, UpdateWorkflowStageConnectionDto updateDto);

    /// <summary>
    /// Deletes a connection by its ID.
    /// </summary>
    /// <param name="connectionId">The ID of the connection to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the connection was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteConnectionAsync(int connectionId);

    /// <summary>
    /// Deletes all connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteConnectionsForStageAsync(int stageId);

    /// <summary>
    /// Gets all incoming connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing incoming connections.</returns>
    Task<IEnumerable<WorkflowStageConnectionDto>> GetIncomingConnectionsAsync(int stageId);

    /// <summary>
    /// Gets all outgoing connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing outgoing connections.</returns>
    Task<IEnumerable<WorkflowStageConnectionDto>> GetOutgoingConnectionsAsync(int stageId);

    /// <summary>
    /// Validates if a connection belongs to stages within the specified workflow.
    /// </summary>
    /// <param name="connectionId">The ID of the connection.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the connection belongs to the workflow, otherwise false.</returns>
    Task<bool> ValidateConnectionBelongsToWorkflowAsync(int connectionId, int workflowId);

    /// <summary>
    /// Validates if the stages in a connection DTO belong to the specified workflow.
    /// </summary>
    /// <param name="createDto">The connection creation DTO.</param>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if both stages belong to the workflow, otherwise false.</returns>
    Task<bool> ValidateConnectionStagesBelongToWorkflowAsync(CreateWorkflowStageConnectionDto createDto, int workflowId);
}
