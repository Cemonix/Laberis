namespace server.Models.Domain.Enums;

public enum WorkflowStageType
{
    // Initial stage - tasks enter here for annotation work
    ANNOTATION,

    // Task is ready for revision after annotation is complete
    REVISION,

    // Revision is complete and accepted - workflow is complete
    COMPLETION,
}
