using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class DataSourceRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assets_data_sources_data_source_id",
                table: "assets");

            migrationBuilder.AlterColumn<int>(
                name: "data_source_id",
                table: "assets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_assets_data_sources_data_source_id",
                table: "assets",
                column: "data_source_id",
                principalTable: "data_sources",
                principalColumn: "data_source_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assets_data_sources_data_source_id",
                table: "assets");

            migrationBuilder.AlterColumn<int>(
                name: "data_source_id",
                table: "assets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_assets_data_sources_data_source_id",
                table: "assets",
                column: "data_source_id",
                principalTable: "data_sources",
                principalColumn: "data_source_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
