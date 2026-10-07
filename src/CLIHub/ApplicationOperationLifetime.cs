namespace CLIHub;

using Microsoft.Extensions.Logging;

/// <summary>
///   Tracks application-scoped asynchronous work and provides a bounded shutdown barrier.
/// </summary>
public sealed class ApplicationOperationLifetime : IApplicationOperationLifetime, IDisposable
{
    /// <summary>
    ///   Default maximum wait used by the production application during shutdown.
    /// </summary>
    public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(2);

    private readonly CancellationTokenSource _cancellation = new();
    private readonly ILogger<ApplicationOperationLifetime> _logger;
    private readonly object _sync = new();
    private readonly HashSet<Task> _operations = [];
    private Task? _stopTask;
    private bool _stopped;
    private bool _disposed;

    /// <summary>
    ///   Creates the application operation lifetime.
    /// </summary>
    /// <param name="logger"> Logger for operation failures, cancellation, and timeouts. </param>
    public ApplicationOperationLifetime(ILogger<ApplicationOperationLifetime> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public CancellationToken Token => _cancellation.Token;

    /// <inheritdoc />
    public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        lock (_sync)
        {
            if (_stopped || _disposed)
            {
                return Task.CompletedTask;
            }

            var task = RunObservedAsync(operationName, operation);
            _operations.Add(task);
            _ = RemoveCompletedAsync(task);
            return task;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        lock (_sync)
        {
            if (_stopTask is not null)
            {
                return _stopTask;
            }

            _stopped = true;
            _cancellation.Cancel();
            _stopTask = StopCoreAsync(timeout, _operations.ToArray());
            return _stopTask;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopped = true;
            _cancellation.Cancel();
            _cancellation.Dispose();
        }
    }

    private async Task RunObservedAsync(
        string operationName,
        Func<CancellationToken, Task> operation)
    {
        try
        {
            await operation(_cancellation.Token);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            _logger.LogInformation("Application operation cancelled: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application operation failed: {OperationName}", operationName);
        }
    }

    private async Task RemoveCompletedAsync(Task operation)
    {
        await operation;

        lock (_sync)
        {
            _operations.Remove(operation);
        }
    }

    private async Task StopCoreAsync(TimeSpan timeout, IReadOnlyCollection<Task> operations)
    {
        if (operations.Count == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(operations).WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Application shutdown timed out while waiting for {OperationCount} operation(s)",
                operations.Count);
        }
    }
}
