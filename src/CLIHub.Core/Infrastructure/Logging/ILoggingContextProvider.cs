namespace CLIHub.Core.Infrastructure.Logging;

/// <summary>
///   Provides structured operation scopes for diagnostic logging.
/// </summary>
public interface ILoggingContextProvider
{
    /// <summary>
    ///   Begins a logging scope with the given operation name and a generated correlation ID.
    /// </summary>
    /// <param name="operationName"> Name of the operation for diagnostic logs. </param>
    /// <returns>
    ///   A disposable scope; all log entries written while the scope is active include the
    ///   operation name and correlation ID as structured properties.
    /// </returns>
    IDisposable BeginOperation(string operationName);
}
