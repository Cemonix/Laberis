using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddLabelSchemeToWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "label_scheme_id",
                table: "workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_workflows_label_scheme_id",
                table: "workflows",
                column: "label_scheme_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflows_label_schemes_label_scheme_id",
                table: "workflows",
                column: "label_scheme_id",
                principalTable: "label_schemes",
                principalColumn: "label_scheme_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflows_label_schemes_label_scheme_id",
                table: "workflows");

            migrationBuilder.DropIndex(
                name: "IX_workflows_label_scheme_id",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "label_scheme_id",
                table: "workflows");
        }
    }
}
