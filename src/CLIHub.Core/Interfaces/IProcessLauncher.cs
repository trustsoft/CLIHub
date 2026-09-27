namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
/// Service for launching processes for AI agent CLI tools
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Launches a process for the specified plugin command in the given project directory
    /// </summary>
    /// <param name="command">The plugin command to execute</param>
    /// <param name="workingDirectory">The working directory for the process</param>
    /// <returns>True if the process was launched successfully, false otherwise</returns>
    bool LaunchProcess(PluginCommand command, string workingDirectory);

    /// <summary>
    /// Runs an executable without an interactive terminal and captures its output.
    /// </summary>
    /// <param name="executable">Executable to run.</param>
    /// <param name="arguments">Optional command-line arguments.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ProcessCaptureResult> CaptureOutputAsync(
        string executable,
        string? arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the terminal executable to use for launching processes
    /// </summary>
    /// <param name="terminalExecutable">Path to the terminal executable</param>
    void SetTerminalExecutable(string terminalExecutable);

    /// <summary>
    /// Gets the configured terminal executable
    /// </summary>
    /// <returns>Path to the terminal executable</returns>
    string GetTerminalExecutable();
}
