using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class AddTaskStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text,line")
                .Annotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,exported,archived")
                .Annotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .Annotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .Annotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .Annotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .Annotation("Npgsql:Enum:public.project_role_enum", "manager,reviewer,annotator,viewer")
                .Annotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .Annotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .Annotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed")
                .Annotation("Npgsql:Enum:public.task_status_enum", "not_started,in_progress,completed,archived,suspended,deferred,ready_for_annotation,ready_for_review,ready_for_completion,changes_required,vetoed")
                .Annotation("Npgsql:Enum:public.workflow_stage_type_enum", "annotation,revision,completion")
                .OldAnnotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text,line")
                .OldAnnotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,exported,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .OldAnnotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .OldAnnotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .OldAnnotation("Npgsql:Enum:public.project_role_enum", "manager,reviewer,annotator,viewer")
                .OldAnnotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .OldAnnotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .OldAnnotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed")
                .OldAnnotation("Npgsql:Enum:public.workflow_stage_type_enum", "annotation,revision,completion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text,line")
                .Annotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,exported,archived")
                .Annotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .Annotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .Annotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .Annotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .Annotation("Npgsql:Enum:public.project_role_enum", "manager,reviewer,annotator,viewer")
                .Annotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .Annotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .Annotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed")
                .Annotation("Npgsql:Enum:public.workflow_stage_type_enum", "annotation,revision,completion")
                .OldAnnotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text,line")
                .OldAnnotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,exported,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .OldAnnotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .OldAnnotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .OldAnnotation("Npgsql:Enum:public.project_role_enum", "manager,reviewer,annotator,viewer")
                .OldAnnotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .OldAnnotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .OldAnnotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed")
                .OldAnnotation("Npgsql:Enum:public.task_status_enum", "not_started,in_progress,completed,archived,suspended,deferred,ready_for_annotation,ready_for_review,ready_for_completion,changes_required,vetoed")
                .OldAnnotation("Npgsql:Enum:public.workflow_stage_type_enum", "annotation,revision,completion");
        }
    }
}
