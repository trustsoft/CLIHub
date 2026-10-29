namespace CLIHub.Core.Tests.Fakes;

using CLIHub.Core.Events;

public class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<object> PublishedEvents { get; } = new();

    public void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent
    {
        this.PublishedEvents.Add(evt!);
    }
}
