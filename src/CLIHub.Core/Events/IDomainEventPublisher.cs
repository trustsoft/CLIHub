namespace CLIHub.Core.Events;

/// <summary>
///   Publishes domain events to registered subscribers.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    ///   Publishes the specified event to all registered subscribers.
    /// </summary>
    /// <typeparam name="TEvent"> The type of event to publish. </typeparam>
    /// <param name="evt"> The event to publish. </param>
    void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent;
}
