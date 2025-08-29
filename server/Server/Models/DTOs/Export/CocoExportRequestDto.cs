using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Export;

/// <summary>
/// Request DTO for COCO export with configuration options
/// </summary>
public class CocoExportRequestDto
{
    /// <summary>
    /// Whether to include ground truth annotations in the export
    /// </summary>
    public bool IncludeGroundTruth { get; set; } = true;

    /// <summary>
    /// Whether to include prediction annotations in the export
    /// </summary>
    public bool IncludePredictions { get; set; } = false;

    /// <summary>
    /// Custom filename for the export (optional)
    /// </summary>
    [StringLength(200, ErrorMessage = "Filename cannot exceed 200 characters")]
    public string? FileName { get; set; }

    /// <summary>
    /// Optional description for the export
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Optional contributor information
    /// </summary>
    [StringLength(100, ErrorMessage = "Contributor cannot exceed 100 characters")]
    public string? Contributor { get; set; }

    /// <summary>
    /// Specific task IDs to include in export (optional - if not provided, all completed tasks are included)
    /// </summary>
    public List<int>? TaskIds { get; set; }

    /// <summary>
    /// Specific label IDs to include in export (optional - if not provided, all labels are included)
    /// </summary>
    public List<int>? LabelIds { get; set; }
}