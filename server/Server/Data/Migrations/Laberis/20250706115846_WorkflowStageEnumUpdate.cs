using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class WorkflowStageEnumUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new enum type
            migrationBuilder.Sql("CREATE TYPE public.workflow_stage_type_enum_new AS ENUM ('annotation', 'revision', 'completion');");

            // Step 2: Add a temporary column with the new enum type
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages ADD COLUMN stage_type_new public.workflow_stage_type_enum_new;");

            // Step 3: Map old values to new values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.workflow_stages 
                SET stage_type_new = CASE 
                    WHEN stage_type = 'annotation' THEN 'annotation'::public.workflow_stage_type_enum_new
                    WHEN stage_type = 'suspended' THEN 'annotation'::public.workflow_stage_type_enum_new
                    WHEN stage_type = 'deferred' THEN 'annotation'::public.workflow_stage_type_enum_new
                    WHEN stage_type = 'review' THEN 'revision'::public.workflow_stage_type_enum_new
                    WHEN stage_type = 'requires_changes' THEN 'revision'::public.workflow_stage_type_enum_new
                    WHEN stage_type = 'accepted' THEN 'completion'::public.workflow_stage_type_enum_new
                    ELSE 'annotation'::public.workflow_stage_type_enum_new
                END
                WHERE stage_type IS NOT NULL;
            ");

            // Step 4: Drop the old column
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages DROP COLUMN stage_type;");

            // Step 5: Rename the new column to the original name
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages RENAME COLUMN stage_type_new TO stage_type;");

            // Step 6: Drop the old enum type
            migrationBuilder.Sql("DROP TYPE public.workflow_stage_type_enum;");

            // Step 7: Rename the new enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.workflow_stage_type_enum_new RENAME TO workflow_stage_type_enum;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the old enum type
            migrationBuilder.Sql("CREATE TYPE public.workflow_stage_type_enum_old AS ENUM ('annotation', 'suspended', 'deferred', 'review', 'requires_changes', 'accepted');");

            // Step 2: Add a temporary column with the old enum type
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages ADD COLUMN stage_type_old public.workflow_stage_type_enum_old;");

            // Step 3: Map new values back to old values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.workflow_stages 
                SET stage_type_old = CASE 
                    WHEN stage_type = 'annotation' THEN 'annotation'::public.workflow_stage_type_enum_old
                    WHEN stage_type = 'revision' THEN 'review'::public.workflow_stage_type_enum_old
                    WHEN stage_type = 'completion' THEN 'accepted'::public.workflow_stage_type_enum_old
                    ELSE 'annotation'::public.workflow_stage_type_enum_old
                END
                WHERE stage_type IS NOT NULL;
            ");

            // Step 4: Drop the new column
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages DROP COLUMN stage_type;");

            // Step 5: Rename the old column to the original name
            migrationBuilder.Sql("ALTER TABLE public.workflow_stages RENAME COLUMN stage_type_old TO stage_type;");

            // Step 6: Drop the new enum type
            migrationBuilder.Sql("DROP TYPE public.workflow_stage_type_enum;");

            // Step 7: Rename the old enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.workflow_stage_type_enum_old RENAME TO workflow_stage_type_enum;");
        }
    }
}
