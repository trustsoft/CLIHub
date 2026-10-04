namespace CLIHub.Tests;

using Microsoft.Extensions.Logging;

using CLIHub;

public sealed class AsyncOperationRunnerTests
{
    [Fact]
    public async Task RunAsync_Success_PreservesCompletionAndDoesNotReportFailure()
    {
        var logger = new CapturingLogger();
        var status = new List<string>();
        var completed = false;

        await AsyncOperationRunner.RunAsync(
            "Test operation",
            () =>
            {
                completed = true;
                return Task.CompletedTask;
            },
            logger,
            status.Add);

        Assert.True(completed);
        Assert.Empty(logger.Errors);
        Assert.Empty(status);
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_LogsAndReportsWithoutEscaping()
    {
        var logger = new CapturingLogger();
        var status = new List<string>();

        var exception = await Record.ExceptionAsync(() => AsyncOperationRunner.RunAsync(
            "Test operation",
            () => Task.FromException(new InvalidOperationException("boom")),
            logger,
            status.Add));

        Assert.Null(exception);
        var error = Assert.Single(logger.Errors);
        Assert.Equal("boom", error.Exception?.Message);
        Assert.Equal("Test operation failed unexpectedly.", Assert.Single(status));
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<LogEntry> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(new LogEntry(exception, formatter(state, exception)));
            }
        }

        public sealed record LogEntry(Exception? Exception, string Message);

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
