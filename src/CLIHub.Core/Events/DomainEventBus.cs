namespace CLIHub.Core.Events;

using Microsoft.Extensions.Logging;

/// <summary>
///   Synchronous domain event bus that dispatches events to subscribers in subscription order.
///   Subscriber exceptions are caught and logged; other subscribers continue to receive the event.
/// </summary>
public sealed class DomainEventBus : IDomainEventPublisher, IDomainEventSubscriber
{
    private readonly ILogger<DomainEventBus> logger;
    private readonly Dictionary<Type, List<Delegate>> subscriptions = new();

    /// <summary>
    ///   Initializes a new instance of the <see cref="DomainEventBus" /> class.
    /// </summary>
    /// <param name="logger"> The logger for subscriber exceptions. </param>
    public DomainEventBus(ILogger<DomainEventBus> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(evt);

        var eventType = typeof(TEvent);
        var correlationId = GenerateCorrelationId();

        if (!this.subscriptions.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.ToList())
        {
            try
            {
                ((Action<TEvent>)handler)(evt);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex,
                    "Subscriber exception during {EventType} dispatch (correlation: {CorrelationId})",
                    eventType.Name,
                    correlationId);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        if (!this.subscriptions.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            this.subscriptions[eventType] = handlers;
        }

        handlers.Add(handler);

        return new Subscription(() =>
        {
            if (this.subscriptions.TryGetValue(eventType, out var currentHandlers))
            {
                currentHandlers.Remove(handler);
            }
        });
    }

    private static string GenerateCorrelationId()
    {
        // 8-character hex from a random int (2^32 space)
        return Random.Shared.Next().ToString("x8");
    }

    private sealed class Subscription : IDisposable
    {
        private Action? unsubscribe;

        public Subscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            this.unsubscribe?.Invoke();
            this.unsubscribe = null;
        }
    }
}
