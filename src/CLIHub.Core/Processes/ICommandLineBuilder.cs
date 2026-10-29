namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// Builds command lines for specific runtimes.
/// </summary>
public interface ICommandLineBuilder
{
    /// <summary>
    /// Builds a command line for interactive terminal execution.
    /// </summary>
    /// <param name="runtime">The runtime executable to use.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <returns>A tuple of (FileName, Arguments) for the process start.</returns>
    (string FileName, string Arguments) BuildInteractive(
        RuntimeExecutable runtime,
        PluginCommand command,
        string workingDirectory);

    /// <summary>
    /// Builds a command line for captured output execution.
    /// </summary>
    /// <param name="executable">The executable to run.</param>
    /// <param name="arguments">The arguments for the executable.</param>
    /// <returns>A tuple of (FileName, Arguments) for the process start.</returns>
    (string FileName, string Arguments) BuildCaptured(
        string executable,
        string? arguments);
}
