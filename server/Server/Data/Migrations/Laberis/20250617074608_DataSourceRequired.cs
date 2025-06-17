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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text")
                .Annotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,ready_for_annotation,annotation_in_progress,annotated,pending_review,review_in_progress,review_accepted,review_rejected,exported,archived")
                .Annotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .Annotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .Annotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .Annotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .Annotation("Npgsql:Enum:public.project_role_enum", "admin,manager,reviewer,annotator,viewer")
                .Annotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .Annotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .Annotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed,task_completed,task_archived")
                .Annotation("Npgsql:Enum:public.workflow_stage_type_enum", "initial_import,preprocessing,annotation,review,quality_assurance,auto_labeling,export,final_acceptance")
                .OldAnnotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text")
                .OldAnnotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,ready_for_annotation,annotation_in_progress,annotated,pending_review,review_in_progress,review_accepted,review_rejected,exported,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .OldAnnotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .OldAnnotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .OldAnnotation("Npgsql:Enum:public.project_role_enum", "admin,manager,reviewer,annotator,viewer")
                .OldAnnotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .OldAnnotation("Npgsql:Enum:public.project_type_enum", "other,image_classification,object_detection,image_segmentation,video_annotation,text_annotation")
                .OldAnnotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed,task_completed,task_archived")
                .OldAnnotation("Npgsql:Enum:public.workflow_stage_type_enum", "initial_import,preprocessing,annotation,review,quality_assurance,auto_labeling,export,final_acceptance");

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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text")
                .Annotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,ready_for_annotation,annotation_in_progress,annotated,pending_review,review_in_progress,review_accepted,review_rejected,exported,archived")
                .Annotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .Annotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .Annotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .Annotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .Annotation("Npgsql:Enum:public.project_role_enum", "admin,manager,reviewer,annotator,viewer")
                .Annotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .Annotation("Npgsql:Enum:public.project_type_enum", "other,image_classification,object_detection,image_segmentation,video_annotation,text_annotation")
                .Annotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed,task_completed,task_archived")
                .Annotation("Npgsql:Enum:public.workflow_stage_type_enum", "initial_import,preprocessing,annotation,review,quality_assurance,auto_labeling,export,final_acceptance")
                .OldAnnotation("Npgsql:Enum:annotation_type_enum", "bounding_box,point,polygon,polyline,text")
                .OldAnnotation("Npgsql:Enum:asset_status_enum", "annotated,annotation_in_progress,archived,exported,import_error,imported,pending_import,pending_processing,pending_review,processing,processing_error,ready_for_annotation,review_accepted,review_in_progress,review_rejected")
                .OldAnnotation("Npgsql:Enum:data_source_status_enum", "active,archived,error,inactive,syncing")
                .OldAnnotation("Npgsql:Enum:data_source_type_enum", "api,azure_blob_storage,database,gsc_bucket,local_directory,minio_bucket,other,s3_bucket")
                .OldAnnotation("Npgsql:Enum:issue_status_enum", "canceled,closed,in_progress,open,reopened,resolved")
                .OldAnnotation("Npgsql:Enum:issue_type_enum", "ambiguous_task,asset_quality_issue,guideline_inquiry,incorrect_annotation,missing_annotation,other")
                .OldAnnotation("Npgsql:Enum:project_role_enum", "admin,annotator,manager,reviewer,viewer")
                .OldAnnotation("Npgsql:Enum:project_status_enum", "active,archived,pending_deletion,read_only")
                .OldAnnotation("Npgsql:Enum:project_type_enum", "image_classification,image_segmentation,object_detection,other,text_annotation,video_annotation")
                .OldAnnotation("Npgsql:Enum:public.annotation_type_enum", "bounding_box,polygon,polyline,point,text")
                .OldAnnotation("Npgsql:Enum:public.asset_status_enum", "pending_import,imported,import_error,pending_processing,processing,processing_error,ready_for_annotation,annotation_in_progress,annotated,pending_review,review_in_progress,review_accepted,review_rejected,exported,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_status_enum", "active,inactive,syncing,error,archived")
                .OldAnnotation("Npgsql:Enum:public.data_source_type_enum", "minio_bucket,s3_bucket,gsc_bucket,azure_blob_storage,local_directory,database,api,other")
                .OldAnnotation("Npgsql:Enum:public.issue_status_enum", "open,in_progress,resolved,closed,reopened,canceled")
                .OldAnnotation("Npgsql:Enum:public.issue_type_enum", "incorrect_annotation,missing_annotation,ambiguous_task,asset_quality_issue,guideline_inquiry,other")
                .OldAnnotation("Npgsql:Enum:public.project_role_enum", "admin,manager,reviewer,annotator,viewer")
                .OldAnnotation("Npgsql:Enum:public.project_status_enum", "active,archived,read_only,pending_deletion")
                .OldAnnotation("Npgsql:Enum:public.project_type_enum", "image_classification,object_detection,image_segmentation,video_annotation,text_annotation,other")
                .OldAnnotation("Npgsql:Enum:public.task_event_type_enum", "task_created,task_assigned,task_unassigned,stage_changed,status_changed,comment_added,annotation_created,annotation_updated,annotation_deleted,review_submitted,issue_raised,priority_changed,due_date_changed,task_completed,task_archived")
                .OldAnnotation("Npgsql:Enum:public.workflow_stage_type_enum", "initial_import,preprocessing,annotation,review,quality_assurance,auto_labeling,export,final_acceptance")
                .OldAnnotation("Npgsql:Enum:task_event_type_enum", "annotation_created,annotation_deleted,annotation_updated,comment_added,due_date_changed,issue_raised,priority_changed,review_submitted,stage_changed,status_changed,task_archived,task_assigned,task_completed,task_created,task_unassigned")
                .OldAnnotation("Npgsql:Enum:workflow_stage_type_enum", "annotation,auto_labeling,export,final_acceptance,initial_import,preprocessing,quality_assurance,review");

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
