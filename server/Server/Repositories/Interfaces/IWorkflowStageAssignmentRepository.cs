using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface IWorkflowStageAssignmentRepository : IGenericRepository<WorkflowStageAssignment>
{
    /// <summary>
    /// Gets all assignments for a specific workflow stage with project member details.
    /// </summary>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <returns>A task that represents the asynchronous operation, containing all assignments for the stage.</returns>
    Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForStageAsync(int workflowStageId);

    /// <summary>
    /// Gets a workflow stage assignment by ID with detailed navigation properties.
    /// </summary>
    /// <param name="assignmentId">The ID of the assignment.</param>
    /// <returns>A task that represents the asynchronous operation, containing the assignment with details if found.</returns>
    Task<WorkflowStageAssignment?> GetByIdWithDetailsAsync(int assignmentId);

    /// <summary>
    /// Gets all assignments for a specific project member.
    /// </summary>
    /// <param name="projectMemberId">The ID of the project member.</param>
    /// <returns>A task that represents the asynchronous operation, containing all assignments for the project member.</returns>
    Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForProjectMemberAsync(int projectMemberId);

    /// <summary>
    /// Gets all assignments for a specific workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow.</param>
    /// <returns>A task that represents the asynchronous operation, containing all assignments for the workflow.</returns>
    Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForWorkflowAsync(int workflowId);

    /// <summary>
    /// Adds multiple assignments in a single operation.
    /// </summary>
    /// <param name="assignments">The assignments to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    System.Threading.Tasks.Task AddRangeAsync(IEnumerable<WorkflowStageAssignment> assignments);

    /// <summary>
    /// Removes multiple assignments in a single operation.
    /// </summary>
    /// <param name="assignments">The assignments to remove.</param>
    void RemoveRange(IEnumerable<WorkflowStageAssignment> assignments);
}
