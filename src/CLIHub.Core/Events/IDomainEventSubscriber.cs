namespace CLIHub.Core.Events;

/// <summary>
///   Subscribes to domain events.
/// </summary>
public interface IDomainEventSubscriber
{
    /// <summary>
    ///   Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent"> The type of event to subscribe to. </typeparam>
    /// <param name="handler"> The handler to invoke when an event is published. </param>
    /// <returns>
    ///   A disposable subscription; dispose to unregister the handler.
    /// </returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent;
}
