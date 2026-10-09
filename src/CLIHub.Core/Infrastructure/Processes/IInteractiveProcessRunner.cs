namespace CLIHub.Core.Infrastructure.Processes;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
///   Launches interactive processes that attach to the user's console.
/// </summary>
public interface IInteractiveProcessRunner
{
    /// <summary>
    ///   Launches an interactive process with the specified command line.
    /// </summary>
    /// <param name="commandLine"> The full command line to execute (already quoted/escaped). </param>
    /// <param name="runtime"> Runtime information specifying the executable and type. </param>
    /// <param name="cancellationToken"> Optional cancellation token. </param>
    /// <returns> The exit code of the launched process. </returns>
    /// <exception cref="System.InvalidOperationException"> Thrown if the process fails to start. </exception>
    /// <exception cref="System.OperationCanceledException"> Thrown if the operation is cancelled. </exception>
    Task<int> LaunchAsync(
        string commandLine,
        RuntimeInfo runtime,
        CancellationToken cancellationToken = default);
}
