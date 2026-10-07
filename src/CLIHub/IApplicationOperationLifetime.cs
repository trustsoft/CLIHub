namespace CLIHub;

/// <summary>
///   Owns application-scoped asynchronous operations and their cancellation lifetime.
/// </summary>
public interface IApplicationOperationLifetime
{
    /// <summary>
    ///   Gets the token cancelled when application shutdown begins.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    ///   Starts and tracks an application-scoped operation.
    /// </summary>
    /// <param name="operationName"> Name used when logging unexpected failures. </param>
    /// <param name="operation"> Operation receiving the application cancellation token. </param>
    /// <returns> A task that completes when the operation has been observed. </returns>
    Task RunAsync(string operationName, Func<CancellationToken, Task> operation);

    /// <summary>
    ///   Cancels tracked operations and waits for them up to the supplied timeout.
    /// </summary>
    /// <param name="timeout"> Maximum time to wait for tracked operations. </param>
    /// <returns> A task that completes after the bounded shutdown wait. </returns>
    Task StopAsync(TimeSpan timeout);
}
