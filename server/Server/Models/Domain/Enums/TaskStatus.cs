namespace server.Models.Domain.Enums;

public enum TaskStatus
{
    NOT_STARTED,
    IN_PROGRESS,
    COMPLETED,
    ARCHIVED,
    SUSPENDED,
    DEFERRED,
    READY_FOR_ANNOTATION,
    READY_FOR_REVIEW,
    READY_FOR_COMPLETION,
    CHANGES_REQUIRED,
    VETOED,
}