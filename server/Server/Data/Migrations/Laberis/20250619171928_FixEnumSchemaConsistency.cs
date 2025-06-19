using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class FixEnumSchemaConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "stage_type",
                table: "workflow_stages",
                type: "public.workflow_stage_type_enum",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "workflow_stage_type_enum",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "event_type",
                table: "task_events",
                type: "public.task_event_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "task_event_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "projects",
                type: "public.project_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "project_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "project_type",
                table: "projects",
                type: "public.project_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "project_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "project_members",
                type: "public.project_role_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "project_role_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "issues",
                type: "public.issue_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "issue_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "issue_type",
                table: "issues",
                type: "public.issue_type_enum",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "issue_type_enum",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "data_sources",
                type: "public.data_source_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "data_source_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "source_type",
                table: "data_sources",
                type: "public.data_source_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "data_source_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "assets",
                type: "public.asset_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "asset_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "annotation_type",
                table: "annotations",
                type: "public.annotation_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "annotation_type_enum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "stage_type",
                table: "workflow_stages",
                type: "workflow_stage_type_enum",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "public.workflow_stage_type_enum",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "event_type",
                table: "task_events",
                type: "task_event_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.task_event_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "projects",
                type: "project_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.project_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "project_type",
                table: "projects",
                type: "project_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.project_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "project_members",
                type: "project_role_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.project_role_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "issues",
                type: "issue_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.issue_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "issue_type",
                table: "issues",
                type: "issue_type_enum",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "public.issue_type_enum",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "data_sources",
                type: "data_source_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.data_source_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "source_type",
                table: "data_sources",
                type: "data_source_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.data_source_type_enum");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "assets",
                type: "asset_status_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.asset_status_enum");

            migrationBuilder.AlterColumn<int>(
                name: "annotation_type",
                table: "annotations",
                type: "annotation_type_enum",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.annotation_type_enum");
        }
    }
}
