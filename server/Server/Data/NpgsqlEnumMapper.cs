using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using server.Models.Domain.Enums;

namespace server.Data;

public static class NpgsqlEnumMapper
{
    /// <summary>
    /// Configures Npgsql to map all C# enums to their corresponding PostgreSQL enum types.
    /// </summary>
    /// <param name="npgsqlOptions">The Npgsql options builder provided by UseNpgsql().</param>
    public static void ConfigureEnums(NpgsqlDbContextOptionsBuilder npgsqlOptions)
    {
        npgsqlOptions.MapEnum<ProjectType>("project_type_enum", "public");
        npgsqlOptions.MapEnum<ProjectStatus>("project_status_enum", "public");
        npgsqlOptions.MapEnum<ProjectRole>("project_role_enum", "public");
        npgsqlOptions.MapEnum<DataSourceType>("data_source_type_enum", "public");
        npgsqlOptions.MapEnum<DataSourceStatus>("data_source_status_enum", "public");
        npgsqlOptions.MapEnum<WorkflowStageType>("workflow_stage_type_enum", "public");
        npgsqlOptions.MapEnum<TaskEventType>("task_event_type_enum", "public");
        npgsqlOptions.MapEnum<IssueStatus>("issue_status_enum", "public");
        npgsqlOptions.MapEnum<IssueType>("issue_type_enum", "public");
        npgsqlOptions.MapEnum<AnnotationType>("annotation_type_enum", "public");
        npgsqlOptions.MapEnum<AssetStatus>("asset_status_enum", "public");
    }
}