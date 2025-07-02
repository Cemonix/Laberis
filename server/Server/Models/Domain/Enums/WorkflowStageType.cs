namespace server.Models.Domain.Enums;

public enum WorkflowStageType
{
    // Initial stage - tasks enter here for annotation work
    ANNOTATION,
    
    // Task is temporarily suspended/paused (not suitable for annotation right now)
    SUSPENDED,
    
    // Task is deferred to be handled later
    DEFERRED,
    
    // Task is ready for review after annotation is complete
    REVIEW,
    
    // Review is complete but requires changes - sent back to annotation
    REQUIRES_CHANGES,
    
    // Review is complete and accepted - workflow is complete
    ACCEPTED,
}
