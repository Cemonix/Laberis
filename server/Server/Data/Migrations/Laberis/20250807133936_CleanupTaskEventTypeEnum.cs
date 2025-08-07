using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class CleanupTaskEventTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TaskEventType Enum Cleanup - Remove redundant status-specific events
            // Step 1: Create the new enum type with cleaned values
            migrationBuilder.Sql("CREATE TYPE public.task_event_type_enum_new AS ENUM ('task_created', 'task_assigned', 'task_unassigned', 'stage_changed', 'status_changed', 'comment_added', 'annotation_created', 'annotation_updated', 'annotation_deleted', 'review_submitted', 'issue_raised', 'priority_changed', 'due_date_changed');");

            // Step 2: Add a temporary column with the new enum type
            migrationBuilder.Sql("ALTER TABLE public.task_events ADD COLUMN event_type_new public.task_event_type_enum_new;");

            // Step 3: Map old values to new values, converting redundant status events to STATUS_CHANGED
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
                    -- Convert removed status-specific events to generic STATUS_CHANGED
                    WHEN event_type IN ('task_completed', 'task_archived', 'task_suspended', 'task_deferred', 'task_reopened') THEN 'status_changed'::public.task_event_type_enum_new
                    ELSE 'status_changed'::public.task_event_type_enum_new
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

            // AssetStatus Enum Cleanup - Remove workflow-related states
            // Step 1: Create the new enum type with cleaned values
            migrationBuilder.Sql("CREATE TYPE public.asset_status_enum_new AS ENUM ('pending_import', 'imported', 'import_error', 'pending_processing', 'processing', 'processing_error', 'exported', 'archived');");

            // Step 2: Add a temporary column with the new enum type
            migrationBuilder.Sql("ALTER TABLE public.assets ADD COLUMN status_new public.asset_status_enum_new;");

            // Step 3: Map old values to new values, converting workflow states to IMPORTED
            migrationBuilder.Sql(@"
                UPDATE public.assets 
                SET status_new = CASE 
                    WHEN status = 'pending_import' THEN 'pending_import'::public.asset_status_enum_new
                    WHEN status = 'imported' THEN 'imported'::public.asset_status_enum_new
                    WHEN status = 'import_error' THEN 'import_error'::public.asset_status_enum_new
                    WHEN status = 'pending_processing' THEN 'pending_processing'::public.asset_status_enum_new
                    WHEN status = 'processing' THEN 'processing'::public.asset_status_enum_new
                    WHEN status = 'processing_error' THEN 'processing_error'::public.asset_status_enum_new
                    WHEN status = 'exported' THEN 'exported'::public.asset_status_enum_new
                    WHEN status = 'archived' THEN 'archived'::public.asset_status_enum_new
                    -- Convert removed workflow states to IMPORTED (they're in the system and available for tasks)
                    WHEN status IN ('ready_for_annotation', 'annotation_in_progress', 'annotated', 'pending_review', 'review_in_progress', 'review_accepted', 'review_rejected') THEN 'imported'::public.asset_status_enum_new
                    ELSE 'imported'::public.asset_status_enum_new
                END
                WHERE status IS NOT NULL;
            ");

            // Step 4: Drop the old column
            migrationBuilder.Sql("ALTER TABLE public.assets DROP COLUMN status;");

            // Step 5: Rename the new column to the original name
            migrationBuilder.Sql("ALTER TABLE public.assets RENAME COLUMN status_new TO status;");

            // Step 6: Drop the old enum type
            migrationBuilder.Sql("DROP TYPE public.asset_status_enum;");

            // Step 7: Rename the new enum type to the original name
            migrationBuilder.Sql("ALTER TYPE public.asset_status_enum_new RENAME TO asset_status_enum;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback AssetStatus Enum - Restore workflow-related states
            migrationBuilder.Sql("CREATE TYPE public.asset_status_enum_old AS ENUM ('pending_import', 'imported', 'import_error', 'pending_processing', 'processing', 'processing_error', 'ready_for_annotation', 'annotation_in_progress', 'annotated', 'pending_review', 'review_in_progress', 'review_accepted', 'review_rejected', 'exported', 'archived');");
            migrationBuilder.Sql("ALTER TABLE public.assets ADD COLUMN status_old public.asset_status_enum_old;");
            migrationBuilder.Sql(@"
                UPDATE public.assets 
                SET status_old = CASE 
                    WHEN status = 'pending_import' THEN 'pending_import'::public.asset_status_enum_old
                    WHEN status = 'imported' THEN 'imported'::public.asset_status_enum_old
                    WHEN status = 'import_error' THEN 'import_error'::public.asset_status_enum_old
                    WHEN status = 'pending_processing' THEN 'pending_processing'::public.asset_status_enum_old
                    WHEN status = 'processing' THEN 'processing'::public.asset_status_enum_old
                    WHEN status = 'processing_error' THEN 'processing_error'::public.asset_status_enum_old
                    WHEN status = 'exported' THEN 'exported'::public.asset_status_enum_old
                    WHEN status = 'archived' THEN 'archived'::public.asset_status_enum_old
                    ELSE 'imported'::public.asset_status_enum_old
                END
                WHERE status IS NOT NULL;
            ");
            migrationBuilder.Sql("ALTER TABLE public.assets DROP COLUMN status;");
            migrationBuilder.Sql("ALTER TABLE public.assets RENAME COLUMN status_old TO status;");
            migrationBuilder.Sql("DROP TYPE public.asset_status_enum;");
            migrationBuilder.Sql("ALTER TYPE public.asset_status_enum_old RENAME TO asset_status_enum;");

            // Rollback TaskEventType Enum - Restore redundant status-specific events  
            migrationBuilder.Sql("CREATE TYPE public.task_event_type_enum_old AS ENUM ('task_created', 'task_assigned', 'task_unassigned', 'stage_changed', 'status_changed', 'comment_added', 'annotation_created', 'annotation_updated', 'annotation_deleted', 'review_submitted', 'issue_raised', 'priority_changed', 'due_date_changed', 'task_completed', 'task_archived', 'task_suspended', 'task_deferred', 'task_reopened');");
            migrationBuilder.Sql("ALTER TABLE public.task_events ADD COLUMN event_type_old public.task_event_type_enum_old;");
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
                    ELSE 'status_changed'::public.task_event_type_enum_old
                END
                WHERE event_type IS NOT NULL;
            ");
            migrationBuilder.Sql("ALTER TABLE public.task_events DROP COLUMN event_type;");
            migrationBuilder.Sql("ALTER TABLE public.task_events RENAME COLUMN event_type_old TO event_type;");
            migrationBuilder.Sql("DROP TYPE public.task_event_type_enum;");
            migrationBuilder.Sql("ALTER TYPE public.task_event_type_enum_old RENAME TO task_event_type_enum;");
        }
    }
}
