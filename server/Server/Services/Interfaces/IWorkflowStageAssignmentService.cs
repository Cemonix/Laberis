using server.Models.DTOs.WorkflowStage;

namespace server.Services.Interfaces;

public interface IWorkflowStageAssignmentService
{
    /// <summary>
    /// Gets all assignments for a specific workflow stage.
    /// </summary>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <returns>A collection of workflow stage assignments.</returns>
    Task<IEnumerable<WorkflowStageAssignmentDto>> GetAssignmentsForStageAsync(int workflowStageId);

    /// <summary>
    /// Creates a new workflow stage assignment.
    /// </summary>
    /// <param name="createDto">The assignment creation data.</param>
    /// <returns>The created workflow stage assignment.</returns>
    Task<WorkflowStageAssignmentDto> CreateAssignmentAsync(CreateWorkflowStageAssignmentDto createDto);

    /// <summary>
    /// Creates multiple assignments for a workflow stage.
    /// </summary>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <param name="projectMemberIds">The IDs of project members to assign.</param>
    /// <returns>The created workflow stage assignments.</returns>
    Task<IEnumerable<WorkflowStageAssignmentDto>> CreateMultipleAssignmentsAsync(int workflowStageId, IEnumerable<int> projectMemberIds);

    /// <summary>
    /// Deletes a workflow stage assignment.
    /// </summary>
    /// <param name="assignmentId">The ID of the assignment to delete.</param>
    /// <returns>True if the assignment was deleted successfully, otherwise false.</returns>
    Task<bool> DeleteAssignmentAsync(int assignmentId);

    /// <summary>
    /// Deletes all assignments for a specific workflow stage.
    /// </summary>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <returns>The number of assignments deleted.</returns>
    Task<int> DeleteAllAssignmentsForStageAsync(int workflowStageId);

    /// <summary>
    /// Validates if a project member can be assigned to a workflow stage.
    /// </summary>
    /// <param name="workflowStageId">The ID of the workflow stage.</param>
    /// <param name="projectMemberId">The ID of the project member.</param>
    /// <returns>True if the assignment is valid, otherwise false.</returns>
    Task<bool> ValidateAssignmentAsync(int workflowStageId, int projectMemberId);
}
