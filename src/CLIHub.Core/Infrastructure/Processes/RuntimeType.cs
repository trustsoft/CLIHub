namespace CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Describes the type of runtime environment used to execute commands.
/// </summary>
public enum RuntimeType
{
    /// <summary>
    ///   Windows Terminal (wt.exe) runtime.
    /// </summary>
    WindowsTerminal,

    /// <summary>
    ///   Command Prompt (cmd.exe) runtime.
    /// </summary>
    Cmd,

    /// <summary>
    ///   PowerShell (powershell.exe) runtime.
    /// </summary>
    PowerShell
}
