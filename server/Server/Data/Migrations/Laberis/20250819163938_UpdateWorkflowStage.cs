using Microsoft.EntityFrameworkCore.Migrations;
using server.Models.Domain.Enums;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class UpdateWorkflowStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ui_configuration",
                table: "workflow_stages");

            migrationBuilder.AlterColumn<WorkflowStageType>(
                name: "stage_type",
                table: "workflow_stages",
                type: "public.workflow_stage_type_enum",
                nullable: false,
                oldClrType: typeof(WorkflowStageType),
                oldType: "public.workflow_stage_type_enum",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<WorkflowStageType>(
                name: "stage_type",
                table: "workflow_stages",
                type: "public.workflow_stage_type_enum",
                nullable: true,
                oldClrType: typeof(WorkflowStageType),
                oldType: "public.workflow_stage_type_enum");

            migrationBuilder.AddColumn<string>(
                name: "ui_configuration",
                table: "workflow_stages",
                type: "jsonb",
                nullable: true);
        }
    }
}
