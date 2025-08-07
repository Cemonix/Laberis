using server.Models.DTOs.DataSource;

namespace server.Models.Internal;

/// <summary>
/// Helper class to organize the data sources needed for workflow stages.
/// Used internally by WorkflowService to manage data source configuration during workflow creation.
/// </summary>
public class WorkflowDataSources
{
    /// <summary>
    /// Data source for annotation stage - contains original assets for initial annotation work.
    /// </summary>
    public DataSourceDto? AnnotationDataSource { get; set; }
    
    /// <summary>
    /// Data source for review stage - contains assets moved from annotation for quality control.
    /// </summary>
    public DataSourceDto? ReviewDataSource { get; set; }
    
    /// <summary>
    /// Data source for completion stage - contains final approved and completed assets.
    /// </summary>
    public DataSourceDto? CompletionDataSource { get; set; }
}