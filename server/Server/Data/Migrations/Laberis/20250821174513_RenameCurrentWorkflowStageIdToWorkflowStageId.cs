using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class RenameCurrentWorkflowStageIdToWorkflowStageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_workflow_stages_current_workflow_stage_id",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "current_workflow_stage_id",
                table: "tasks",
                newName: "workflow_stage_id");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_current_workflow_stage_id",
                table: "tasks",
                newName: "IX_tasks_workflow_stage_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_workflow_stages_workflow_stage_id",
                table: "tasks",
                column: "workflow_stage_id",
                principalTable: "workflow_stages",
                principalColumn: "workflow_stage_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_workflow_stages_workflow_stage_id",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "workflow_stage_id",
                table: "tasks",
                newName: "current_workflow_stage_id");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_workflow_stage_id",
                table: "tasks",
                newName: "IX_tasks_current_workflow_stage_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_workflow_stages_current_workflow_stage_id",
                table: "tasks",
                column: "current_workflow_stage_id",
                principalTable: "workflow_stages",
                principalColumn: "workflow_stage_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
