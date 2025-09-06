using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddTaskLockingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lock_expires_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_by_user_id",
                table: "tasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tasks_locked_by_user_id",
                table: "tasks",
                column: "locked_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_AspNetUsers_locked_by_user_id",
                table: "tasks",
                column: "locked_by_user_id",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_AspNetUsers_locked_by_user_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_locked_by_user_id",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "lock_expires_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "locked_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "locked_by_user_id",
                table: "tasks");
        }
    }
}
