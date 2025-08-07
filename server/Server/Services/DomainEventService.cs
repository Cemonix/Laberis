using server.Services.Interfaces;

namespace server.Services;

/// <summary>
/// In-memory implementation of domain events service
/// This provides a clean way to decouple services without circular dependencies
/// </summary>
public class DomainEventService : IDomainEventService
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<DomainEventService> _logger;

    public DomainEventService(ILogger<DomainEventService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<T>(T domainEvent) where T : IDomainEvent
    {
        var eventType = typeof(T);
        
        _logger.LogDebug("Publishing domain event: {EventType} with ID: {EventId}", 
            eventType.Name, domainEvent.EventId);

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type: {EventType}", eventType.Name);
            return;
        }

        var typedHandlers = handlers.Cast<IDomainEventHandler<T>>().ToList();
        
        _logger.LogInformation("Found {HandlerCount} handlers for event {EventType}", 
            typedHandlers.Count, eventType.Name);

        foreach (var handler in typedHandlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent);
                _logger.LogDebug("Successfully handled event {EventId} with handler {HandlerType}", 
                    domainEvent.EventId, handler.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventId} with handler {HandlerType}", 
                    domainEvent.EventId, handler.GetType().Name);
                // Continue processing other handlers even if one fails
            }
        }
    }

    public void Subscribe<T>(IDomainEventHandler<T> handler) where T : IDomainEvent
    {
        var eventType = typeof(T);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers = [];
            _handlers[eventType] = handlers;
        }
        
        handlers.Add(handler);
        
        _logger.LogInformation("Registered handler {HandlerType} for event type {EventType}", 
            handler.GetType().Name, eventType.Name);
    }
}