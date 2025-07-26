using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AnnotationTypeEnumUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new enum type
            migrationBuilder.Sql("CREATE TYPE public.annotation_type_enum_new AS ENUM ('bounding_box', 'polygon', 'polyline', 'point', 'text', 'line');");

            // Step 2: Add a temporary column with the new enum type
            migrationBuilder.Sql("ALTER TABLE public.annotations ADD COLUMN annotation_type_new public.annotation_type_enum_new;");

            // Step 3: Map old values to new values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.annotations 
                SET annotation_type_new = CASE 
                    WHEN annotation_type = 'bounding_box' THEN 'bounding_box'::public.annotation_type_enum_new
                    WHEN annotation_type = 'polygon' THEN 'polygon'::public.annotation_type_enum_new
                    WHEN annotation_type = 'polyline' THEN 'polyline'::public.annotation_type_enum_new
                    WHEN annotation_type = 'point' THEN 'point'::public.annotation_type_enum_new
                    WHEN annotation_type = 'text' THEN 'text'::public.annotation_type_enum_new
                    ELSE 'bounding_box'::public.annotation_type_enum_new
                END
                WHERE annotation_type IS NOT NULL;
            ");

            // Step 4: Drop the old column
            migrationBuilder.Sql("ALTER TABLE public.annotations DROP COLUMN annotation_type;");

            // Step 5: Rename the new column to the original name
            migrationBuilder.Sql("ALTER TABLE public.annotations RENAME COLUMN annotation_type_new TO annotation_type;");

            // Step 6: Drop the old enum type
            migrationBuilder.Sql("DROP TYPE public.annotation_type_enum;");

            // Step 7: Rename the new enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.annotation_type_enum_new RENAME TO annotation_type_enum;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the old enum type
            migrationBuilder.Sql("CREATE TYPE public.annotation_type_enum_old AS ENUM ('bounding_box', 'polygon', 'polyline', 'point', 'text');");

            // Step 2: Add a temporary column with the old enum type
            migrationBuilder.Sql("ALTER TABLE public.annotations ADD COLUMN annotation_type_old public.annotation_type_enum_old;");

            // Step 3: Map new values back to old values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.annotations 
                SET annotation_type_old = CASE 
                    WHEN annotation_type = 'bounding_box' THEN 'bounding_box'::public.annotation_type_enum_old
                    WHEN annotation_type = 'polygon' THEN 'polygon'::public.annotation_type_enum_old
                    WHEN annotation_type = 'polyline' THEN 'polyline'::public.annotation_type_enum_old
                    WHEN annotation_type = 'point' THEN 'point'::public.annotation_type_enum_old
                    WHEN annotation_type = 'text' THEN 'text'::public.annotation_type_enum_old
                    WHEN annotation_type = 'line' THEN 'polyline'::public.annotation_type_enum_old
                    ELSE 'bounding_box'::public.annotation_type_enum_old
                END
                WHERE annotation_type IS NOT NULL;
            ");

            // Step 4: Drop the new column
            migrationBuilder.Sql("ALTER TABLE public.annotations DROP COLUMN annotation_type;");

            // Step 5: Rename the old column to the original name
            migrationBuilder.Sql("ALTER TABLE public.annotations RENAME COLUMN annotation_type_old TO annotation_type;");

            // Step 6: Drop the new enum type
            migrationBuilder.Sql("DROP TYPE public.annotation_type_enum;");

            // Step 7: Rename the old enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.annotation_type_enum_old RENAME TO annotation_type_enum;");
        }
    }
}
