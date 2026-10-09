namespace CLIHub.Core.Infrastructure.Processes;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
///   Runs processes with output capture and timeout support.
/// </summary>
public interface IProcessOutputRunner
{
    /// <summary>
    ///   Runs a process and captures its output with a specified timeout.
    /// </summary>
    /// <param name="commandLine"> The full command line to execute (already quoted/escaped). </param>
    /// <param name="runtime"> Runtime information specifying the executable and type. </param>
    /// <param name="timeout"> Maximum execution time before the process is killed. </param>
    /// <param name="cancellationToken"> Optional cancellation token. </param>
    /// <returns> A ProcessResult containing output, error, exit code, and timeout flag. </returns>
    /// <exception cref="System.InvalidOperationException"> Thrown if the process fails to start. </exception>
    /// <exception cref="System.OperationCanceledException"> Thrown if the operation is cancelled. </exception>
    Task<ProcessResult> RunAsync(
        string commandLine,
        RuntimeInfo runtime,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
