using CLIHub.Core.Models;

namespace CLIHub.Core.Interfaces;

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