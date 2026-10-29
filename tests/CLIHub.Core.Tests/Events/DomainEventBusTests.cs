namespace CLIHub.Core.Tests.Events;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Events;

public class DomainEventBusTests
{
    private record TestEvent(string Value) : IDomainEvent;
    private record AnotherEvent(int Number) : IDomainEvent;

    [Fact]
    public void Publish_InvokesSubscribedHandlers()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var received = new List<TestEvent>();

        bus.Subscribe<TestEvent>(evt => received.Add(evt));
        bus.Publish(new TestEvent("test"));

        Assert.Single(received);
        Assert.Equal("test", received[0].Value);
    }

    [Fact]
    public void Publish_InvokesMultipleHandlersInSubscriptionOrder()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var order = new List<int>();

        bus.Subscribe<TestEvent>(_ => order.Add(1));
        bus.Subscribe<TestEvent>(_ => order.Add(2));
        bus.Publish(new TestEvent("test"));

        Assert.Equal(new[] { 1, 2 }, order);
    }

    [Fact]
    public void Publish_DoesNotInvokeUnsubscribedHandlers()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var invoked = false;

        var subscription = bus.Subscribe<TestEvent>(_ => invoked = true);
        subscription.Dispose();
        bus.Publish(new TestEvent("test"));

        Assert.False(invoked);
    }

    [Fact]
    public void Publish_LogsAndContinuesOnSubscriberException()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var secondHandlerInvoked = false;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("Test exception"));
        bus.Subscribe<TestEvent>(_ => secondHandlerInvoked = true);
        bus.Publish(new TestEvent("test"));

        Assert.True(secondHandlerInvoked);
    }

    [Fact]
    public void Subscribe_ReturnsSeparateDisposablesForDifferentHandlers()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var first = false;
        var second = false;

        var subscription1 = bus.Subscribe<TestEvent>(_ => first = true);
        var subscription2 = bus.Subscribe<TestEvent>(_ => second = true);
        subscription1.Dispose();
        bus.Publish(new TestEvent("test"));

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);

        var exception = Record.Exception(() => bus.Publish(new TestEvent("test")));

        Assert.Null(exception);
    }
}
