namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
/// Adapts <see cref="WindowsCommandLineBuilder"/> to provide command line building.
/// </summary>
internal sealed class CommandLineBuilderAdapter : ICommandLineBuilder
{
    /// <inheritdoc/>
    public (string FileName, string Arguments) BuildInteractive(
        RuntimeExecutable runtime,
        PluginCommand command,
        string workingDirectory)
    {
        // Extract resolved executables for GitBash and PowerShell7
        var gitBashExe = runtime.Kind == RuntimeKind.GitBash ? runtime.ExecutablePath : null;
        var ps7Exe = runtime.Kind == RuntimeKind.PowerShell7 ? runtime.ExecutablePath : null;

        return WindowsCommandLineBuilder.BuildInteractive(
            runtime.Kind,
            command,
            workingDirectory,
            gitBashExe,
            ps7Exe);
    }

    /// <inheritdoc/>
    public (string FileName, string Arguments) BuildCaptured(
        string executable,
        string? arguments)
    {
        return WindowsCommandLineBuilder.BuildCapturedCommand(executable, arguments);
    }
}
