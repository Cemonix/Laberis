using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class UpdateSoftDeleteConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_labels_label_scheme_id_name",
                table: "labels");

            migrationBuilder.DropIndex(
                name: "IX_label_schemes_project_id_name",
                table: "label_schemes");

            migrationBuilder.RenameColumn(
                name: "OriginalName",
                table: "labels",
                newName: "original_name");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "labels",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "labels",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "label_schemes",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "label_schemes",
                newName: "deleted_at");

            migrationBuilder.AlterColumn<string>(
                name: "original_name",
                table: "labels",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "labels",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "label_schemes",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateIndex(
                name: "IX_labels_label_scheme_id_name_is_active",
                table: "labels",
                columns: new[] { "label_scheme_id", "name", "is_active" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_label_schemes_project_id_name_is_active",
                table: "label_schemes",
                columns: new[] { "project_id", "name", "is_active" },
                unique: true,
                filter: "is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_labels_label_scheme_id_name_is_active",
                table: "labels");

            migrationBuilder.DropIndex(
                name: "IX_label_schemes_project_id_name_is_active",
                table: "label_schemes");

            migrationBuilder.RenameColumn(
                name: "original_name",
                table: "labels",
                newName: "OriginalName");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "labels",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "labels",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "label_schemes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "label_schemes",
                newName: "DeletedAt");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalName",
                table: "labels",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "labels",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "label_schemes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_labels_label_scheme_id_name",
                table: "labels",
                columns: new[] { "label_scheme_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_label_schemes_project_id_name",
                table: "label_schemes",
                columns: new[] { "project_id", "name" },
                unique: true);
        }
    }
}
