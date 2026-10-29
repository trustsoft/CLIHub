namespace CLIHub.Core.Tests.Infrastructure.Logging;

using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Logging;

using Xunit;

public class LoggingContextProviderTests
{
    [Fact]
    public void BeginOperation_CreatesScope_WithOperationNameAndCorrelationId()
    {
        // Arrange
        var recordingLogger = new RecordingLogger<LoggingContextProvider>();
        var provider = new LoggingContextProvider(recordingLogger);

        // Act
        using (var scope = provider.BeginOperation("TestOperation"))
        {
            // Assert
            Assert.Single(recordingLogger.Scopes);
            var scopeDict = recordingLogger.Scopes[0] as Dictionary<string, object>;
            Assert.NotNull(scopeDict);
            Assert.Equal("TestOperation", scopeDict["OperationName"]);
            Assert.Contains("CorrelationId", scopeDict.Keys);
            Assert.IsType<string>(scopeDict["CorrelationId"]);
            var correlationId = (string)scopeDict["CorrelationId"];
            Assert.Equal(8, correlationId.Length);
        }
    }

    [Fact]
    public void BeginOperation_MultipleCalls_CreateDistinctCorrelationIds()
    {
        // Arrange
        var recordingLogger = new RecordingLogger<LoggingContextProvider>();
        var provider = new LoggingContextProvider(recordingLogger);

        // Act
        string? correlationId1;
        string? correlationId2;

        using (var scope1 = provider.BeginOperation("Operation1"))
        {
            var scopeDict1 = recordingLogger.Scopes[0] as Dictionary<string, object>;
            correlationId1 = scopeDict1?["CorrelationId"] as string;
        }

        using (var scope2 = provider.BeginOperation("Operation2"))
        {
            var scopeDict2 = recordingLogger.Scopes[1] as Dictionary<string, object>;
            correlationId2 = scopeDict2?["CorrelationId"] as string;
        }

        // Assert
        Assert.NotNull(correlationId1);
        Assert.NotNull(correlationId2);
        Assert.NotEqual(correlationId1, correlationId2);
    }

    [Fact]
    public void BeginOperation_Dispose_EndsScope()
    {
        // Arrange
        var recordingLogger = new RecordingLogger<LoggingContextProvider>();
        var provider = new LoggingContextProvider(recordingLogger);

        // Act
        var scope = provider.BeginOperation("TestOperation");
        Assert.Single(recordingLogger.Scopes);
        Assert.Equal(1, recordingLogger.ActiveScopeCount);

        scope.Dispose();

        // Assert
        Assert.Equal(0, recordingLogger.ActiveScopeCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BeginOperation_EmptyOrWhiteSpaceOperationName_ThrowsArgumentException(string operationName)
    {
        // Arrange
        var provider = new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.BeginOperation(operationName));
    }

    [Fact]
    public void BeginOperation_NullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.BeginOperation(null!));
    }

    /// <summary>
    ///   Recording logger for testing scope creation.
    /// </summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<object?> Scopes { get; } = new();
        public int ActiveScopeCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            this.Scopes.Add(state);
            this.ActiveScopeCount++;
            var scope = new RecordedScope(() => this.ActiveScopeCount--);
            return scope;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Not needed for scope tests
        }

        private sealed class RecordedScope : IDisposable
        {
            private readonly Action onDispose;

            public RecordedScope(Action onDispose)
            {
                this.onDispose = onDispose;
            }

            public void Dispose()
            {
                this.onDispose();
            }
        }
    }
}
