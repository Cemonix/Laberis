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
        npgsqlOptions.MapEnum<ProjectType>("public.project_type_enum");
        npgsqlOptions.MapEnum<ProjectStatus>("public.project_status_enum");
        npgsqlOptions.MapEnum<ProjectRole>("public.project_role_enum");
        npgsqlOptions.MapEnum<DataSourceType>("public.data_source_type_enum");
        npgsqlOptions.MapEnum<DataSourceStatus>("public.data_source_status_enum");
        npgsqlOptions.MapEnum<WorkflowStageType>("public.workflow_stage_type_enum");
        npgsqlOptions.MapEnum<TaskEventType>("public.task_event_type_enum");
        npgsqlOptions.MapEnum<IssueStatus>("public.issue_status_enum");
        npgsqlOptions.MapEnum<IssueType>("public.issue_type_enum");
        npgsqlOptions.MapEnum<AnnotationType>("public.annotation_type_enum");
        npgsqlOptions.MapEnum<AssetStatus>("public.asset_status_enum");
    }
}