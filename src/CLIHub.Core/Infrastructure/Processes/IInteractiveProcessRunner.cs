namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Starts interactive agent commands in the configured terminal runtime.
/// </summary>
public interface IInteractiveProcessRunner
{
    /// <summary>
    ///   Launches a process for the specified plugin command in the given project directory.
    /// </summary>
    /// <param name="command"> The plugin command to execute. </param>
    /// <param name="workingDirectory"> The working directory for the process. </param>
    /// <returns> True when the process was launched successfully; otherwise false. </returns>
    bool LaunchProcess(PluginCommand command, string workingDirectory);

    /// <summary>
    ///   Sets the runtime used to launch interactive agent commands.
    /// </summary>
    /// <param name="runtime"> The runtime to use. </param>
    void SetRuntime(RuntimeKind runtime);
}
