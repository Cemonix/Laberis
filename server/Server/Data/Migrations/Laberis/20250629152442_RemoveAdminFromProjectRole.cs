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
            // Step 1: Rename the old enum type
            migrationBuilder.Sql(@"ALTER TYPE ""project_role_enum"" RENAME TO ""project_role_enum_old"";");
            
            // Step 2: Create the new enum type with updated values
            migrationBuilder.Sql(@"CREATE TYPE ""project_role_enum"" AS ENUM ('manager', 'reviewer', 'annotator', 'viewer');");
            
            // Step 3: Update all columns that use the old enum to use the new enum
            // Update project_members table
            migrationBuilder.Sql(@"ALTER TABLE ""project_members"" ALTER COLUMN ""role"" TYPE ""project_role_enum"" USING ""role""::text::""project_role_enum"";");
            
            // Update project_invitations table if it exists
            migrationBuilder.Sql(@"ALTER TABLE ""project_invitations"" ALTER COLUMN ""role"" TYPE ""project_role_enum"" USING ""role""::text::""project_role_enum"";");
            
            // Step 4: Drop the old enum type (now safe to do so)
            migrationBuilder.Sql(@"DROP TYPE ""project_role_enum_old"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Rename the current enum type
            migrationBuilder.Sql(@"ALTER TYPE ""project_role_enum"" RENAME TO ""project_role_enum_new"";");
            
            // Step 2: Recreate the old enum type with ADMIN
            migrationBuilder.Sql(@"CREATE TYPE ""project_role_enum"" AS ENUM ('ADMIN', 'manager', 'reviewer', 'annotator', 'viewer');");
            
            // Step 3: Update all columns back to use the old enum
            // Update project_members table
            migrationBuilder.Sql(@"ALTER TABLE ""project_members"" ALTER COLUMN ""role"" TYPE ""project_role_enum"" USING ""role""::text::""project_role_enum"";");
            
            // Update project_invitations table if it exists
            migrationBuilder.Sql(@"ALTER TABLE ""project_invitations"" ALTER COLUMN ""role"" TYPE ""project_role_enum"" USING ""role""::text::""project_role_enum"";");
            
            // Step 4: Drop the new enum type
            migrationBuilder.Sql(@"DROP TYPE ""project_role_enum_new"";");
        }
    }
}
