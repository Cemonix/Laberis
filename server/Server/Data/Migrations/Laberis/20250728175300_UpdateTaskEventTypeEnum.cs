using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class UpdateTaskEventTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new enum type with all values
            migrationBuilder.Sql("CREATE TYPE public.task_event_type_enum_new AS ENUM ('task_created', 'task_assigned', 'task_unassigned', 'stage_changed', 'status_changed', 'comment_added', 'annotation_created', 'annotation_updated', 'annotation_deleted', 'review_submitted', 'issue_raised', 'priority_changed', 'due_date_changed', 'task_completed', 'task_archived', 'task_suspended', 'task_deferred', 'task_reopened');");

            // Step 2: Add a temporary column with the new enum type
            migrationBuilder.Sql("ALTER TABLE public.task_events ADD COLUMN event_type_new public.task_event_type_enum_new;");

            // Step 3: Map old values to new values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.task_events 
                SET event_type_new = CASE 
                    WHEN event_type = 'task_created' THEN 'task_created'::public.task_event_type_enum_new
                    WHEN event_type = 'task_assigned' THEN 'task_assigned'::public.task_event_type_enum_new
                    WHEN event_type = 'task_unassigned' THEN 'task_unassigned'::public.task_event_type_enum_new
                    WHEN event_type = 'stage_changed' THEN 'stage_changed'::public.task_event_type_enum_new
                    WHEN event_type = 'status_changed' THEN 'status_changed'::public.task_event_type_enum_new
                    WHEN event_type = 'comment_added' THEN 'comment_added'::public.task_event_type_enum_new
                    WHEN event_type = 'annotation_created' THEN 'annotation_created'::public.task_event_type_enum_new
                    WHEN event_type = 'annotation_updated' THEN 'annotation_updated'::public.task_event_type_enum_new
                    WHEN event_type = 'annotation_deleted' THEN 'annotation_deleted'::public.task_event_type_enum_new
                    WHEN event_type = 'review_submitted' THEN 'review_submitted'::public.task_event_type_enum_new
                    WHEN event_type = 'issue_raised' THEN 'issue_raised'::public.task_event_type_enum_new
                    WHEN event_type = 'priority_changed' THEN 'priority_changed'::public.task_event_type_enum_new
                    WHEN event_type = 'due_date_changed' THEN 'due_date_changed'::public.task_event_type_enum_new
                    WHEN event_type = 'task_completed' THEN 'task_completed'::public.task_event_type_enum_new
                    WHEN event_type = 'task_archived' THEN 'task_archived'::public.task_event_type_enum_new
                    ELSE 'task_created'::public.task_event_type_enum_new
                END
                WHERE event_type IS NOT NULL;
            ");

            // Step 4: Drop the old column
            migrationBuilder.Sql("ALTER TABLE public.task_events DROP COLUMN event_type;");

            // Step 5: Rename the new column to the original name
            migrationBuilder.Sql("ALTER TABLE public.task_events RENAME COLUMN event_type_new TO event_type;");

            // Step 6: Drop the old enum type
            migrationBuilder.Sql("DROP TYPE public.task_event_type_enum;");

            // Step 7: Rename the new enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.task_event_type_enum_new RENAME TO task_event_type_enum;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the old enum type (only the original values)
            migrationBuilder.Sql("CREATE TYPE public.task_event_type_enum_old AS ENUM ('task_created', 'task_assigned', 'task_unassigned', 'stage_changed', 'status_changed', 'comment_added', 'annotation_created', 'annotation_updated', 'annotation_deleted', 'review_submitted', 'issue_raised', 'priority_changed', 'due_date_changed', 'task_completed', 'task_archived');");

            // Step 2: Add a temporary column with the old enum type
            migrationBuilder.Sql("ALTER TABLE public.task_events ADD COLUMN event_type_old public.task_event_type_enum_old;");

            // Step 3: Map new values back to old values and update the temporary column
            migrationBuilder.Sql(@"
                UPDATE public.task_events 
                SET event_type_old = CASE 
                    WHEN event_type = 'task_created' THEN 'task_created'::public.task_event_type_enum_old
                    WHEN event_type = 'task_assigned' THEN 'task_assigned'::public.task_event_type_enum_old
                    WHEN event_type = 'task_unassigned' THEN 'task_unassigned'::public.task_event_type_enum_old
                    WHEN event_type = 'stage_changed' THEN 'stage_changed'::public.task_event_type_enum_old
                    WHEN event_type = 'status_changed' THEN 'status_changed'::public.task_event_type_enum_old
                    WHEN event_type = 'comment_added' THEN 'comment_added'::public.task_event_type_enum_old
                    WHEN event_type = 'annotation_created' THEN 'annotation_created'::public.task_event_type_enum_old
                    WHEN event_type = 'annotation_updated' THEN 'annotation_updated'::public.task_event_type_enum_old
                    WHEN event_type = 'annotation_deleted' THEN 'annotation_deleted'::public.task_event_type_enum_old
                    WHEN event_type = 'review_submitted' THEN 'review_submitted'::public.task_event_type_enum_old
                    WHEN event_type = 'issue_raised' THEN 'issue_raised'::public.task_event_type_enum_old
                    WHEN event_type = 'priority_changed' THEN 'priority_changed'::public.task_event_type_enum_old
                    WHEN event_type = 'due_date_changed' THEN 'due_date_changed'::public.task_event_type_enum_old
                    WHEN event_type = 'task_completed' THEN 'task_completed'::public.task_event_type_enum_old
                    WHEN event_type = 'task_archived' THEN 'task_archived'::public.task_event_type_enum_old
                    -- Map new values to appropriate old values
                    WHEN event_type = 'task_suspended' THEN 'task_archived'::public.task_event_type_enum_old
                    WHEN event_type = 'task_deferred' THEN 'task_archived'::public.task_event_type_enum_old
                    WHEN event_type = 'task_reopened' THEN 'task_created'::public.task_event_type_enum_old
                    ELSE 'task_created'::public.task_event_type_enum_old
                END
                WHERE event_type IS NOT NULL;
            ");

            // Step 4: Drop the new column
            migrationBuilder.Sql("ALTER TABLE public.task_events DROP COLUMN event_type;");

            // Step 5: Rename the old column to the original name
            migrationBuilder.Sql("ALTER TABLE public.task_events RENAME COLUMN event_type_old TO event_type;");

            // Step 6: Drop the new enum type
            migrationBuilder.Sql("DROP TYPE public.task_event_type_enum;");

            // Step 7: Rename the old enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.task_event_type_enum_old RENAME TO task_event_type_enum;");
        }
    }
}
