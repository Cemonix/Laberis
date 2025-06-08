using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Data.Migrations.Laberis
{
    /// <inheritdoc />
    public partial class CreateUpdatedAtFunctionAndTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION trigger_set_timestamp()
                RETURNS TRIGGER AS $$
                BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create trigger for table Projects
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_projects
                BEFORE UPDATE ON projects
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table ProjectMembers
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_project_members
                BEFORE UPDATE ON project_members
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Labels
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_labels
                BEFORE UPDATE ON labels
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table LabelSchemes
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_label_schemes
                BEFORE UPDATE ON label_schemes
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Tasks
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_tasks
                BEFORE UPDATE ON tasks
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Annotations
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_annotations
                BEFORE UPDATE ON annotations
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Assets
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_assets
                BEFORE UPDATE ON assets
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table DataSources
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_data_sources
                BEFORE UPDATE ON data_sources
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Issues
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_issues
                BEFORE UPDATE ON issues
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table Workflows
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_workflows
                BEFORE UPDATE ON workflows
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");

            // Create trigger for table WorkflowStages
            migrationBuilder.Sql(@"
                CREATE TRIGGER set_timestamp_workflow_stages
                BEFORE UPDATE ON workflow_stages
                FOR EACH ROW
                EXECUTE FUNCTION trigger_set_timestamp();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_projects ON projects;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_project_members ON project_members;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_labels ON labels;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_label_schemes ON label_schemes;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_tasks ON tasks;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_annotations ON annotations;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_assets ON assets;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_data_sources ON data_sources;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_issues ON issues;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_workflows ON workflows;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS set_timestamp_workflow_stages ON workflow_stages;");

            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS trigger_set_timestamp();");
        }
    }
}
