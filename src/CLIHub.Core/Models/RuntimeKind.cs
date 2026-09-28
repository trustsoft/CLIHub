namespace CLIHub.Core.Models;

/// <summary>
/// The shell/terminal used to run interactive agent commands.
/// </summary>
public enum RuntimeKind
{
    /// <summary>Windows Terminal (default).</summary>
    WindowsTerminal,

    /// <summary>Windows Command Prompt.</summary>
    CommandPrompt,

    /// <summary>PowerShell.</summary>
    PowerShell
}

/// <summary>
/// Maps <see cref="RuntimeKind"/> to and from its stored token and resolves legacy
/// terminal-executable values.
/// </summary>
public static class RuntimeKinds
{
    public const string WindowsTerminalToken = "wt";
    public const string CommandPromptToken = "cmd";
    public const string PowerShellToken = "ps";

    /// <summary>
    /// Parses a runtime token or a legacy terminal executable name. Returns false for
    /// null, empty, or unknown values.
    /// </summary>
    public static bool TryParse(string? value, out RuntimeKind kind)
    {
        kind = RuntimeKind.WindowsTerminal;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "wt":
            case "winterminal":
            case "windows-terminal":
            case "windows terminal":
            case "wt.exe":
                kind = RuntimeKind.WindowsTerminal;
                return true;
            case "cmd":
            case "cmd.exe":
            case "command prompt":
            case "commandprompt":
                kind = RuntimeKind.CommandPrompt;
                return true;
            case "ps":
            case "pwsh":
            case "pwsh.exe":
            case "powershell":
            case "powershell.exe":
                kind = RuntimeKind.PowerShell;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Parses a runtime token, falling back to <see cref="RuntimeKind.WindowsTerminal"/>.
    /// </summary>
    public static RuntimeKind Parse(string? value) =>
        TryParse(value, out var kind) ? kind : RuntimeKind.WindowsTerminal;

    /// <summary>
    /// Returns the stored token for a runtime.
    /// </summary>
    public static string ToToken(RuntimeKind kind) => kind switch
    {
        RuntimeKind.CommandPrompt => CommandPromptToken,
        RuntimeKind.PowerShell => PowerShellToken,
        _ => WindowsTerminalToken
    };
}
