namespace CLIHub.Core.Tests.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CLIHub.Core;
using CLIHub.Core.Events;
using CLIHub.Core.Models;

using Xunit;

public class EventSubscriberActivationTests
{
    [Fact]
    public void ResolvingPublisher_ActivatesSubscribers_AndEventsReachAuditLog()
    {
        // Arrange — the real Core composition with a recording log provider.
        var recorder = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(recorder);
        });
        services.AddClIHubCoreServices();

        using var provider = services.BuildServiceProvider();

        // Act — resolving the publisher must activate both subscriber singletons; without that
        // activation the subscribers are never constructed and never subscribe (the defect the
        // manual smoke exposed).
        var publisher = provider.GetRequiredService<IDomainEventPublisher>();
        publisher.Publish(new AgentLaunched(
            "opencode",
            @"D:\code\proj",
            AgentCommandKind.Launch,
            DateTimeOffset.UtcNow));
        publisher.Publish(new ProjectSelected(
            "proj-id",
            @"D:\code\proj",
            DateTimeOffset.UtcNow));

        // Assert — both audit lines reached the log through the real bus and subscribers.
        Assert.Contains(recorder.Messages, message => message.Contains("Agent launched", StringComparison.Ordinal));
        Assert.Contains(recorder.Messages, message => message.Contains("Project selected", StringComparison.Ordinal));
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> messages = new();

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (this.messages)
                {
                    return this.messages.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this);

        public void Dispose()
        {
        }

        private void Record(string message)
        {
            lock (this.messages)
            {
                this.messages.Add(message);
            }
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly RecordingLoggerProvider owner;

            public RecordingLogger(RecordingLoggerProvider owner)
            {
                this.owner = owner;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!this.IsEnabled(logLevel))
                {
                    return;
                }

                this.owner.Record(formatter(state, exception));
            }
        }
    }
}
