namespace CLIHub;

using Microsoft.Extensions.Logging;

/// <summary>
///   Provides an exception boundary for asynchronous work started by synchronous UI handlers.
/// </summary>
public static class AsyncOperationRunner
{
    /// <summary>
    ///   Runs an asynchronous operation and reports unexpected failures without allowing them to
    ///   escape the fire-and-forget caller.
    /// </summary>
    /// <param name="operationName"> Name used in logs and the user-facing failure message. </param>
    /// <param name="operation"> Asynchronous operation to execute. </param>
    /// <param name="logger"> Logger receiving the exception and operation context. </param>
    /// <param name="reportStatus"> Optional callback for a concise user-facing failure message. </param>
    /// <param name="cancellationToken"> Token used to distinguish expected cancellation. </param>
    public static async Task RunAsync(
        string operationName,
        Func<Task> operation,
        ILogger logger,
        Action<string>? reportStatus = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected exception in fire-and-forget operation {OperationName}", operationName);
            reportStatus?.Invoke($"{operationName} failed unexpectedly.");
        }
    }
}
