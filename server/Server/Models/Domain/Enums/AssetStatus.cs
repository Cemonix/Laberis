namespace server.Models.Domain.Enums;

public enum AssetStatus
{
    PENDING_IMPORT,
    IMPORTED,
    IMPORT_ERROR,
    PENDING_PROCESSING,
    PROCESSING,
    PROCESSING_ERROR,
    EXPORTED,
    ARCHIVED
}
