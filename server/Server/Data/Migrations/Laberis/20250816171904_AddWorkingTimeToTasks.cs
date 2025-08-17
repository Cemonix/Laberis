using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddWorkingTimeToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeferredAt",
                table: "tasks",
                newName: "deferred_at");

            migrationBuilder.AddColumn<long>(
                name: "working_time_ms",
                table: "tasks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "working_time_ms",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "deferred_at",
                table: "tasks",
                newName: "DeferredAt");
        }
    }
}
