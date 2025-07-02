using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class WorkflowStageUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserWorkflowStage");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "workflow_stages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workflow_stage_assignments",
                columns: table => new
                {
                    workflow_stage_assignment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workflow_stage_id = table.Column<int>(type: "integer", nullable: false),
                    project_member_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_stage_assignments", x => x.workflow_stage_assignment_id);
                    table.ForeignKey(
                        name: "FK_workflow_stage_assignments_project_members_project_member_id",
                        column: x => x.project_member_id,
                        principalTable: "project_members",
                        principalColumn: "project_member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_stage_assignments_workflow_stages_workflow_stage_id",
                        column: x => x.workflow_stage_id,
                        principalTable: "workflow_stages",
                        principalColumn: "workflow_stage_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_stage_connections",
                columns: table => new
                {
                    workflow_stage_connection_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_stage_id = table.Column<int>(type: "integer", nullable: false),
                    to_stage_id = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_stage_connections", x => x.workflow_stage_connection_id);
                    table.ForeignKey(
                        name: "FK_workflow_stage_connections_workflow_stages_from_stage_id",
                        column: x => x.from_stage_id,
                        principalTable: "workflow_stages",
                        principalColumn: "workflow_stage_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_stage_connections_workflow_stages_to_stage_id",
                        column: x => x.to_stage_id,
                        principalTable: "workflow_stages",
                        principalColumn: "workflow_stage_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stages_ApplicationUserId",
                table: "workflow_stages",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stage_assignments_project_member_id",
                table: "workflow_stage_assignments",
                column: "project_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stage_assignments_workflow_stage_id_project_member~",
                table: "workflow_stage_assignments",
                columns: new[] { "workflow_stage_id", "project_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stage_connections_from_stage_id_to_stage_id_condit~",
                table: "workflow_stage_connections",
                columns: new[] { "from_stage_id", "to_stage_id", "condition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stage_connections_to_stage_id",
                table: "workflow_stage_connections",
                column: "to_stage_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_stages_AspNetUsers_ApplicationUserId",
                table: "workflow_stages",
                column: "ApplicationUserId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_stages_AspNetUsers_ApplicationUserId",
                table: "workflow_stages");

            migrationBuilder.DropTable(
                name: "workflow_stage_assignments");

            migrationBuilder.DropTable(
                name: "workflow_stage_connections");

            migrationBuilder.DropIndex(
                name: "IX_workflow_stages_ApplicationUserId",
                table: "workflow_stages");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "workflow_stages");

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
    }
}
