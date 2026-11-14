namespace CLIHub.Core.Tests.Events;

using System.Collections.Concurrent;

using CLIHub.Core.Events;

using Microsoft.Extensions.Logging.Abstractions;

public class DomainEventBusTests
{
    private record TestEvent(string Value) : IDomainEvent;

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

    [Fact]
    public async Task ConcurrentPublishSubscribeUnsubscribe_NoExceptionEscapesAndOrderPreserved()
    {
        // Test scenario: concurrent publishers, subscribers, and unsubscribers racing on the same
        // event type, with 1000+ deterministic publishes, asserting no exception escapes.
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var publisherCount = 4;
        var publishCountPerPublisher = 250; // 1000 total publishes
        var stableSubscriberCount = 3;
        var exceptions = new List<Exception>();

        // Per-publisher state: each stable handler records values it receives from each publisher
        // into a separate list, so we can verify per-publisher order without data races.
        // Handlers run synchronously on the publishing thread, so each publisher's list has one writer.
        var perPublisherReceivedValues = new ConcurrentDictionary<int, List<int>>[stableSubscriberCount];
        var totalInvocationCount = new int[stableSubscriberCount];

        // Subscribe stable handlers that will never be disposed during the test
        var stableSubscriptions = new List<IDisposable>();
        for (int subscriberIndex = 0; subscriberIndex < stableSubscriberCount; subscriberIndex++)
        {
            var subIdx = subscriberIndex; // Capture for closure
            perPublisherReceivedValues[subIdx] = new ConcurrentDictionary<int, List<int>>();

            var subscription = bus.Subscribe<TestEvent>(evt =>
            {
                // Event.Value format: "publisher-{publisherId}-value-{value}"
                var parts = evt.Value.Split('-');
                var publisherId = int.Parse(parts[1]);
                var value = int.Parse(parts[3]);

                // Record this value for this publisher
                var list = perPublisherReceivedValues[subIdx].GetOrAdd(publisherId, _ => new List<int>());
                list.Add(value);
                Interlocked.Increment(ref totalInvocationCount[subIdx]);
            });
            stableSubscriptions.Add(subscription);
        }

        // Run concurrent publishers: each publishes 0..N-1 on its own thread
        var publisherTasks = new List<Task>();
        for (int publisherId = 0; publisherId < publisherCount; publisherId++)
        {
            var pubId = publisherId; // Capture for closure
            var task = Task.Run(() =>
            {
                try
                {
                    for (int value = 0; value < publishCountPerPublisher; value++)
                    {
                        bus.Publish(new TestEvent($"publisher-{pubId}-value-{value}"));
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            publisherTasks.Add(task);
        }

        // Concurrently subscribe and dispose TestEvent handlers (same event type as publishers)
        // to exercise the actual race: Publish's snapshot racing Subscribe/Dispose mutation
        var concurrentOpsTask = Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 200; i++)
                {
                    // Subscribe a temporary handler to TestEvent (same type publishers use)
                    var tempSub = bus.Subscribe<TestEvent>(_ => { });
                    // Immediately unsubscribe
                    tempSub.Dispose();
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Wait for all tasks with a timeout (deadlock detection)
        var allTasks = publisherTasks.Append(concurrentOpsTask).ToArray();
        try
        {
            await Task.WhenAll(allTasks).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            Assert.Fail("Tasks did not complete within timeout (possible deadlock)");
        }

        // No exception should have been captured
        Assert.Empty(exceptions);

        // Each stable subscriber should have received every value from every publisher exactly once
        var expectedTotalPerSubscriber = publisherCount * publishCountPerPublisher;
        for (int subIdx = 0; subIdx < stableSubscriberCount; subIdx++)
        {
            Assert.Equal(expectedTotalPerSubscriber, totalInvocationCount[subIdx]);

            // Verify per-publisher order: each publisher's values should arrive in strict ascending order
            for (int pubId = 0; pubId < publisherCount; pubId++)
            {
                var receivedValues = perPublisherReceivedValues[subIdx][pubId];

                // Should have received all values 0..publishCountPerPublisher-1 from this publisher
                Assert.Equal(publishCountPerPublisher, receivedValues.Count);
                for (int expected = 0; expected < publishCountPerPublisher; expected++)
                {
                    Assert.Equal(expected, receivedValues[expected]);
                }
            }
        }

        bus.Dispose();
    }
}
