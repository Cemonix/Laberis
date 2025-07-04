using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface IWorkflowStageConnectionRepository : IGenericRepository<WorkflowStageConnection>
{
    /// <summary>
    /// Gets all connections for a specific workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, containing all connections for the workflow.</returns>
    Task<IEnumerable<WorkflowStageConnection>> GetConnectionsForWorkflowAsync(int workflowId);

    /// <summary>
    /// Gets all incoming connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing incoming connections.</returns>
    Task<IEnumerable<WorkflowStageConnection>> GetIncomingConnectionsAsync(int stageId);

    /// <summary>
    /// Gets all outgoing connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing outgoing connections.</returns>
    Task<IEnumerable<WorkflowStageConnection>> GetOutgoingConnectionsAsync(int stageId);

    /// <summary>
    /// Deletes all connections for a specific stage.
    /// </summary>
    /// <param name="stageId">The ID of the stage.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    System.Threading.Tasks.Task DeleteConnectionsForStageAsync(int stageId);
}
