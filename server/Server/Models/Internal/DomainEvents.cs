using server.Services.Interfaces;
using server.Models.Domain;

namespace server.Models.Internal;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; }
    public string EventId { get; }

    protected DomainEvent()
    {
        EventId = Guid.NewGuid().ToString();
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event published when an asset is imported and ready for task creation
/// </summary>
public class AssetImportedEvent : DomainEvent
{
    public Asset Asset { get; }
    
    public AssetImportedEvent(Asset asset)
    {
        Asset = asset ?? throw new ArgumentNullException(nameof(asset));
    }
}

/// <summary>
/// Event published when an asset needs to be moved to a different data source during workflow progression
/// </summary>
public class AssetWorkflowMovementRequestEvent : DomainEvent
{
    public int AssetId { get; }
    public int FromDataSourceId { get; }
    public int ToDataSourceId { get; }
    public int TargetWorkflowStageId { get; }
    public string UserId { get; }

    public AssetWorkflowMovementRequestEvent(
        int assetId, 
        int fromDataSourceId, 
        int toDataSourceId, 
        int targetWorkflowStageId,
        string userId)
    {
        AssetId = assetId;
        FromDataSourceId = fromDataSourceId;
        ToDataSourceId = toDataSourceId;
        TargetWorkflowStageId = targetWorkflowStageId;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    }
}