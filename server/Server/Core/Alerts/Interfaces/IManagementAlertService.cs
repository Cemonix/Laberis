using server.Core.Alerts.Models;

namespace server.Core.Alerts.Interfaces;

/// <summary>
/// Service responsible for managing critical system alerts that require management intervention.
/// </summary>
public interface IManagementAlertService
{
    /// <summary>
    /// Creates a new management alert for a pipeline failure.
    /// </summary>
    /// <param name="type">The type of alert to create.</param>
    /// <param name="taskId">The task ID involved in the failure.</param>
    /// <param name="assetId">The asset ID involved in the failure.</param>
    /// <param name="userId">The user ID who initiated the failed operation.</param>
    /// <param name="failureReason">Description of why the operation failed.</param>
    /// <param name="originalError">Detailed error message from the original operation.</param>
    /// <param name="rollbackError">Details about the rollback failure, if applicable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created management alert.</returns>
    Task<ManagementAlert> CreateAlertAsync(
        AlertType type,
        int taskId,
        int assetId,
        string userId,
        string failureReason,
        string originalError,
        string? rollbackError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends immediate notifications for a critical alert.
    /// This includes email notifications to admins and logging at critical level.
    /// </summary>
    /// <param name="alert">The alert to send notifications for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task SendCriticalNotificationsAsync(ManagementAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert with management notes.
    /// </summary>
    /// <param name="alertId">The ID of the alert to resolve.</param>
    /// <param name="resolvedByUserId">The user ID of the manager resolving the alert.</param>
    /// <param name="resolutionNotes">Notes about how the alert was resolved.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the alert was resolved successfully, false if not found.</returns>
    Task<bool> ResolveAlertAsync(
        int alertId,
        string resolvedByUserId,
        string resolutionNotes,
        CancellationToken cancellationToken = default);
}