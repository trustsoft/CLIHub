namespace CLIHub.Tests.Composition;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moq;

using CLIHub.Composition;
using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;
using CLIHub.Tests.Services;

using Xunit;

public class EventCompositionTests
{
    [Fact]
    public void AddClIHubServices_ResolvingPublisher_ActivatesSubscribers_AndEventsReachAuditLog()
    {
        // Arrange — the full application composition with a recording log provider.
        var recorder = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(recorder);
        });
        services.AddClIHubServices();
        services.ReplaceGuardWithUniqueNames();

        using var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredService<Core.Events.IDomainEventPublisher>();
        publisher.Publish(new Core.Events.AgentLaunched(
            "opencode",
            @"D:\code\proj",
            AgentCommandKind.Launch,
            DateTimeOffset.UtcNow));

        // Assert — the app composition must activate subscribers exactly like the Core composition.
        Assert.Contains(recorder.Messages, message => message.Contains("Agent launched", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddClIHubServices_RealAgentCommandService_PublishesLaunchEventToAuditLog()
    {
        // Arrange — full app composition; only the process boundary is faked so a real
        // AgentCommandService from DI executes the launch path end-to-end.
        var recorder = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(recorder);
        });
        services.AddClIHubServices();
        services.ReplaceGuardWithUniqueNames();

        var orchestrator = new Mock<IProcessOrchestrator>();
        orchestrator
            .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Success(0));
        services.RemoveAll<IProcessOrchestrator>();
        services.AddSingleton(orchestrator.Object);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IAgentCommandService>();

        var plugin = new Plugin
        {
            Id = "opencode",
            Name = "OpenCode",
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = "opencode" }
            }
        };

        // Act
        var result = await service.ExecuteAsync(plugin, AgentCommandKind.Launch, Path.GetTempPath());

        // Assert
        Assert.True(result.Success);
        Assert.Contains(recorder.Messages, message => message.Contains("Agent launched", StringComparison.Ordinal));
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
