using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class RemoveAdminFromProjectRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TYPE ""project_role_enum"" RENAME TO ""project_role_enum_old"";");
            migrationBuilder.Sql(@"CREATE TYPE ""project_role_enum"" AS ENUM ('MANAGER', 'REVIEWER', 'ANNOTATOR', 'VIEWER');");
            migrationBuilder.Sql(@"DROP TYPE ""project_role_enum_old"" CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
