using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using server.Models.Domain.Enums;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class ProjectInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_invitations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<ProjectRole>(type: "public.project_role_enum", nullable: false),
                    invitation_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    invited_by_user_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_invitations_AspNetUsers_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_invitations_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_email_project_id",
                table: "project_invitations",
                columns: new[] { "email", "project_id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_expires_at",
                table: "project_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_invitation_token",
                table: "project_invitations",
                column: "invitation_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_invited_by_user_id",
                table: "project_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_project_id",
                table: "project_invitations",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_invitations");
        }
    }
}
