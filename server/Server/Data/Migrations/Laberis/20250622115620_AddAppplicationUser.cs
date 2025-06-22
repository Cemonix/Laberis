using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddAppplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUserWorkflowStage",
                columns: table => new
                {
                    AssignedUsersId = table.Column<string>(type: "text", nullable: false),
                    WorkflowStagesWorkflowStageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserWorkflowStage", x => new { x.AssignedUsersId, x.WorkflowStagesWorkflowStageId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserWorkflowStage_AspNetUsers_AssignedUsersId",
                        column: x => x.AssignedUsersId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserWorkflowStage_workflow_stages_WorkflowStages~",
                        column: x => x.WorkflowStagesWorkflowStageId,
                        principalTable: "workflow_stages",
                        principalColumn: "workflow_stage_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserWorkflowStage_WorkflowStagesWorkflowStageId",
                table: "ApplicationUserWorkflowStage",
                column: "WorkflowStagesWorkflowStageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserWorkflowStage");
        }
    }
}
