namespace server.Models.Domain.Enums;

public enum WorkflowStageType
{
    INITIAL_IMPORT,
    PREPROCESSING,
    ANNOTATION,
    REVIEW,
    QUALITY_ASSURANCE,
    AUTO_LABELING,
    EXPORT,
    FINAL_ACCEPTANCE
}
