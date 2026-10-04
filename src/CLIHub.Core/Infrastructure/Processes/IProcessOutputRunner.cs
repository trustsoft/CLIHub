namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Runs commands without an interactive terminal and captures their output.
/// </summary>
public interface IProcessOutputRunner
{
    /// <summary>
    ///   Runs an executable and captures its output.
    /// </summary>
    /// <param name="executable"> Executable to run. </param>
    /// <param name="arguments"> Optional command-line arguments. </param>
    /// <param name="workingDirectory"> Working directory for the process. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <param name="timeout"> Optional maximum run time. </param>
    Task<ProcessCaptureResult> CaptureOutputAsync(
        string executable,
        string? arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null);
}
