using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Services.Interfaces;

public interface ITaskStatusValidator
{
    /// <summary>
    /// Validates whether a task status transition is allowed based on current state and workflow rules.
    /// </summary>
    /// <param name="task">The task to validate the transition for.</param>
    /// <param name="currentStatus">The current calculated status of the task.</param>
    /// <param name="targetStatus">The desired target status.</param>
    /// <param name="userId">The ID of the user attempting the transition (for role-based validation).</param>
    /// <returns>A validation result indicating if the transition is valid and any error message.</returns>
    (bool IsValid, string ErrorMessage) ValidateStatusTransition(
        server.Models.Domain.Task task, 
        server.Models.Domain.Enums.TaskStatus currentStatus, 
        server.Models.Domain.Enums.TaskStatus targetStatus, 
        string userId);
}