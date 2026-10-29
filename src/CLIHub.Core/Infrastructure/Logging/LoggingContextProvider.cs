namespace CLIHub.Core.Infrastructure.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
///   Default implementation of <see cref="ILoggingContextProvider" /> using Microsoft.Extensions.Logging.
/// </summary>
public sealed class LoggingContextProvider : ILoggingContextProvider
{
    private readonly ILogger<LoggingContextProvider> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="LoggingContextProvider" /> class.
    /// </summary>
    /// <param name="logger"> The logger used for scope creation. </param>
    public LoggingContextProvider(ILogger<LoggingContextProvider> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IDisposable BeginOperation(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var correlationId = GenerateCorrelationId();
        var scope = new Dictionary<string, object>
        {
            ["OperationName"] = operationName,
            ["CorrelationId"] = correlationId
        };

        return this.logger.BeginScope(scope) ?? NoOpDisposable.Instance;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }

    private static string GenerateCorrelationId()
    {
        // 8-character hex from a random int (2^32 space)
        return Random.Shared.Next().ToString("x8");
    }
}
