using server.Models.DTOs.Export;

namespace server.Services.Interfaces;

/// <summary>
/// Service for exporting annotated data in various formats
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports completed tasks from a workflow stage in COCO format
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">The workflow stage ID to export from</param>
    /// <param name="includeGroundTruth">Whether to include ground truth annotations</param>
    /// <param name="includePredictions">Whether to include prediction annotations</param>
    /// <returns>COCO dataset as byte array (JSON)</returns>
    Task<byte[]> ExportCocoFormatAsync(int projectId, int workflowStageId, bool includeGroundTruth = true, bool includePredictions = false);

    /// <summary>
    /// Gets export metadata for a workflow stage
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">The workflow stage ID</param>
    /// <returns>Export metadata including task counts and available formats</returns>
    Task<ExportMetadataDto> GetExportMetadataAsync(int projectId, int workflowStageId);
}