using Npgsql;
using server.Models.Domain.Enums;

namespace server.Data;

public static class NpgsqlEnumMapper
{
    public static void MapEnums(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.MapEnum<ProjectType>("project_type_enum");
        dataSourceBuilder.MapEnum<ProjectStatus>("project_status_enum");
        dataSourceBuilder.MapEnum<ProjectRole>("project_role_enum");
        dataSourceBuilder.MapEnum<DataSourceType>("data_source_type_enum");
        dataSourceBuilder.MapEnum<DataSourceStatus>("data_source_status_enum");
        dataSourceBuilder.MapEnum<WorkflowStageType>("workflow_stage_type_enum");
        dataSourceBuilder.MapEnum<TaskEventType>("task_event_type_enum");
        dataSourceBuilder.MapEnum<IssueStatus>("issue_status_enum");
        dataSourceBuilder.MapEnum<IssueType>("issue_type_enum");
        dataSourceBuilder.MapEnum<AnnotationType>("annotation_type_enum");
        dataSourceBuilder.MapEnum<AssetStatus>("asset_status_enum");
    }
}