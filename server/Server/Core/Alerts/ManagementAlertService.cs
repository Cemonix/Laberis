using server.Core.Alerts.Interfaces;
using server.Core.Alerts.Models;
using Microsoft.Extensions.Logging;

namespace server.Core.Alerts;

/// <summary>
/// Service responsible for managing critical system alerts that require management intervention.
/// Currently implements logging-based alerts. Can be extended with database storage and email notifications.
/// </summary>
public class ManagementAlertService : IManagementAlertService
{
    private readonly ILogger<ManagementAlertService> _logger;

    public ManagementAlertService(ILogger<ManagementAlertService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new management alert for a pipeline failure.
    /// </summary>
    public async Task<ManagementAlert> CreateAlertAsync(
        AlertType type,
        int taskId,
        int assetId,
        string userId,
        string failureReason,
        string originalError,
        string? rollbackError = null,
        CancellationToken cancellationToken = default)
    {
        var alert = new ManagementAlert
        {
            AlertId = Random.Shared.Next(1000, 9999), // Simple ID generation for now
            Type = type,
            TaskId = taskId,
            AssetId = assetId,
            UserId = userId,
            FailureReason = failureReason,
            OriginalError = originalError,
            RollbackError = rollbackError,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        // Log the alert at critical level
        _logger.LogCritical(
            "MANAGEMENT ALERT CREATED: {AlertType} for Task {TaskId}, Asset {AssetId} by User {UserId}. " +
            "Reason: {FailureReason}. Original Error: {OriginalError}. Rollback Error: {RollbackError}",
            type, taskId, assetId, userId, failureReason, originalError, rollbackError);

        // Send critical notifications immediately
        await SendCriticalNotificationsAsync(alert, cancellationToken);

        return alert;
    }

    /// <summary>
    /// Sends immediate notifications for a critical alert.
    /// This includes email notifications to admins and logging at critical level.
    /// </summary>
    public async Task SendCriticalNotificationsAsync(ManagementAlert alert, CancellationToken cancellationToken = default)
    {
        // For now, log the critical notification
        // TODO: Implement email notifications to administrators
        // TODO: Implement Slack/Teams notifications
        // TODO: Implement database storage for alert tracking

        _logger.LogCritical(
            "CRITICAL ALERT NOTIFICATION: {AlertType} Alert #{AlertId} requires immediate management attention. " +
            "Task: {TaskId}, Asset: {AssetId}, User: {UserId}, Failure: {FailureReason}",
            alert.Type, alert.AlertId, alert.TaskId, alert.AssetId, alert.UserId, alert.FailureReason);

        // Simulate async operation
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resolves an alert with management notes.
    /// </summary>
    public async Task<bool> ResolveAlertAsync(
        int alertId,
        string resolvedByUserId,
        string resolutionNotes,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement database storage and retrieval
        // For now, just log the resolution

        _logger.LogInformation(
            "Management Alert #{AlertId} resolved by {ResolvedByUserId}. Resolution notes: {ResolutionNotes}",
            alertId, resolvedByUserId, resolutionNotes);

        // Simulate async operation
        await Task.CompletedTask;

        return true; // Always return true for now since we don't have database storage
    }
}