namespace server.Models.DTOs.Export;

/// <summary>
/// Metadata about available export data for a workflow stage
/// </summary>
public class ExportMetadataDto
{
    /// <summary>
    /// Number of completed tasks available for export
    /// </summary>
    public int CompletedTasksCount { get; set; }

    /// <summary>
    /// Number of annotations available for export
    /// </summary>
    public int AnnotationsCount { get; set; }

    /// <summary>
    /// Number of unique assets with annotations
    /// </summary>
    public int AnnotatedAssetsCount { get; set; }

    /// <summary>
    /// Number of unique labels/categories
    /// </summary>
    public int CategoriesCount { get; set; }

    /// <summary>
    /// Workflow stage name
    /// </summary>
    public string WorkflowStageName { get; set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Available export formats
    /// </summary>
    public List<string> AvailableFormats { get; set; } = ["COCO"];
}