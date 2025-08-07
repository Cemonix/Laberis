namespace server.Services.Interfaces;

/// <summary>
/// Service for publishing and handling domain events
/// </summary>
public interface IDomainEventService
{
    /// <summary>
    /// Publish a domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
    
    /// <summary>
    /// Register an event handler
    /// </summary>
    /// <param name="handler">The event handler</param>
    void Subscribe<T>(IDomainEventHandler<T> handler) where T : IDomainEvent;
}

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
    string EventId { get; }
}

/// <summary>
/// Interface for domain event handlers
/// </summary>
public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent);
}