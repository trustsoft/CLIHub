namespace CLIHub.Core.Events;

using Microsoft.Extensions.Logging;

/// <summary>
///   Synchronous domain event bus that dispatches events to subscribers in subscription order.
///   Subscriber exceptions are caught and logged; other subscribers continue to receive the event.
///   Access to the subscription map is synchronized with a <see cref="ReaderWriterLockSlim" />, so
///   publishes from background threads are safe alongside concurrent subscribe and unsubscribe
///   calls; handlers run outside the lock.
/// </summary>
public sealed class DomainEventBus : IDomainEventPublisher, IDomainEventSubscriber, IDisposable
{
    private readonly ILogger<DomainEventBus> logger;
    private readonly Dictionary<Type, List<Delegate>> subscriptions = new();
    private readonly ReaderWriterLockSlim subscriptionsGate = new(LockRecursionPolicy.NoRecursion);

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

        // Snapshot the handler list under read lock, then invoke outside the lock.
        List<Delegate>? handlersSnapshot = null;
        this.subscriptionsGate.EnterReadLock();
        try
        {
            if (this.subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlersSnapshot = handlers.ToList();
            }
        }
        finally
        {
            this.subscriptionsGate.ExitReadLock();
        }

        if (handlersSnapshot is null)
        {
            return;
        }

        foreach (var handler in handlersSnapshot)
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

        this.subscriptionsGate.EnterWriteLock();
        try
        {
            if (!this.subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                this.subscriptions[eventType] = handlers;
            }

            handlers.Add(handler);
        }
        finally
        {
            this.subscriptionsGate.ExitWriteLock();
        }

        return new Subscription(() =>
        {
            try
            {
                this.subscriptionsGate.EnterWriteLock();
            }
            catch (ObjectDisposedException)
            {
                // The bus has been disposed; the subscription lists are no longer needed.
                return;
            }

            try
            {
                if (this.subscriptions.TryGetValue(eventType, out var currentHandlers))
                {
                    currentHandlers.Remove(handler);
                }
            }
            finally
            {
                this.subscriptionsGate.ExitWriteLock();
            }
        });
    }

    private static string GenerateCorrelationId()
    {
        // 8-character hex from a random int (2^32 space)
        return Random.Shared.Next().ToString("x8");
    }

    /// <summary>
    ///   Disposes the reader/writer lock that protects the subscription map.
    ///   After disposal, new calls to <see cref="Publish{TEvent}" /> and <see cref="Subscribe{TEvent}" />
    ///   will throw an <see cref="ObjectDisposedException" />. Outstanding subscriptions whose
    ///   <see cref="Dispose" /> method is called after the bus is disposed will silently no-op
    ///   (catching and ignoring the lock's <see cref="ObjectDisposedException" />), so teardown is safe
    ///   even if subscribers are cleaned up after the bus is disposed.
    /// </summary>
    public void Dispose()
    {
        this.subscriptionsGate.Dispose();
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
