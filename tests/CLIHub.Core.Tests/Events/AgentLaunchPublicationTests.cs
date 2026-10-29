namespace CLIHub.Core.Tests.Events;

using Microsoft.Extensions.Logging;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

using Xunit;

public class AgentLaunchPublicationTests
{
    [Fact]
    public async Task ExecuteAsync_SuccessfulLaunch_PublishReachesAuditSubscriber()
    {
        // Arrange — real bus + real subscriber with a recording logger; only the process layer is faked.
        var recorder = new RecordingLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(recorder);
        });

        var bus = new DomainEventBus(loggerFactory.CreateLogger<DomainEventBus>());
        var loggingContext = new LoggingContextProvider(
            loggerFactory.CreateLogger<LoggingContextProvider>());
        var subscriber = new AgentLaunchAuditSubscriber(
            bus,
            loggingContext,
            loggerFactory.CreateLogger<AgentLaunchAuditSubscriber>());

        using (subscriber)
        {
            var orchestrator = new Mock<IProcessOrchestrator>();
            orchestrator
                .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
                .ReturnsAsync(ProcessResult.Success(0));

            var preferences = new Mock<IPreferencesStore>();
            preferences.Setup(x => x.Load()).Returns(new AppPreferences());

            var service = new AgentCommandService(
                orchestrator.Object,
                preferences.Object,
                loggerFactory.CreateLogger<AgentCommandService>(),
                bus);

            var plugin = new Plugin
            {
                Id = "opencode",
                Name = "OpenCode",
                Commands = new AgentCommands
                {
                    Launch = new PluginCommand { Executable = "opencode" }
                }
            };
            var projectPath = Path.GetTempPath();

            // Act
            var result = await service.ExecuteAsync(plugin, AgentCommandKind.Launch, projectPath);

            // Assert — the success path must publish and reach the subscriber's audit line.
            Assert.True(result.Success);
            Assert.Contains(recorder.Messages, message => message.Contains("Agent launched", StringComparison.Ordinal));
        }
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
