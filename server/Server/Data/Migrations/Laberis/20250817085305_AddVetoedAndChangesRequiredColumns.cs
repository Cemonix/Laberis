using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddVetoedAndChangesRequiredColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "changes_required_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "vetoed_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "changes_required_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "vetoed_at",
                table: "tasks");
        }
    }
}
