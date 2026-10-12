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

    private readonly CancellationTokenSource cancellation = new();
    private readonly ILogger<ApplicationOperationLifetime> logger;
    private readonly object sync = new();
    private readonly HashSet<Task> operations = [];
    private Task? stopTask;
    private bool stopped;
    private bool disposed;

    /// <summary>
    ///   Creates the application operation lifetime.
    /// </summary>
    /// <param name="logger"> Logger for operation failures, cancellation, and timeouts. </param>
    public ApplicationOperationLifetime(ILogger<ApplicationOperationLifetime> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public CancellationToken Token => this.cancellation.Token;

    /// <inheritdoc />
    public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        lock (this.sync)
        {
            if (this.stopped || this.disposed)
            {
                return Task.CompletedTask;
            }

            var task = this.RunObservedAsync(operationName, operation);
            this.operations.Add(task);
            _ = this.RemoveCompletedAsync(task);
            return task;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        lock (this.sync)
        {
            if (this.stopTask is not null)
            {
                return this.stopTask;
            }

            this.stopped = true;
            this.cancellation.Cancel();
            this.stopTask = this.StopCoreAsync(timeout, this.operations.ToArray());
            return this.stopTask;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (this.sync)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.stopped = true;
            this.cancellation.Cancel();
            this.cancellation.Dispose();
        }
    }

    private async Task RunObservedAsync(
        string operationName,
        Func<CancellationToken, Task> operation)
    {
        try
        {
            await operation(this.cancellation.Token);
        }
        catch (OperationCanceledException) when (this.cancellation.IsCancellationRequested)
        {
            this.logger.LogInformation("Application operation cancelled: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Application operation failed: {OperationName}", operationName);
        }
    }

    private async Task RemoveCompletedAsync(Task operation)
    {
        await operation;

        lock (this.sync)
        {
            this.operations.Remove(operation);
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
            this.logger.LogWarning(
                "Application shutdown timed out while waiting for {OperationCount} operation(s)",
                operations.Count);
        }
    }
}
