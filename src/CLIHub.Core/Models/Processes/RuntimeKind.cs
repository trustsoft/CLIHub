namespace CLIHub.Core.Models;

/// <summary>
///   The shell/terminal used to run interactive agent commands. The host is the shell.
/// </summary>
public enum RuntimeKind
{
    /// <summary>
    ///   Windows PowerShell (powershell.exe). The default.
    /// </summary>
    PowerShell,

    /// <summary>
    ///   PowerShell 7 (pwsh.exe).
    /// </summary>
    PowerShell7,

    /// <summary>
    ///   Windows Command Prompt (cmd.exe).
    /// </summary>
    CommandPrompt,

    /// <summary>
    ///   Git Bash (bash.exe).
    /// </summary>
    GitBash
}

/// <summary>
///   Maps <see cref="RuntimeKind" /> to and from its stored token.
/// </summary>
public static class RuntimeKinds
{
    /// <summary>
    ///   The configuration token for Windows PowerShell.
    /// </summary>
    public const string PowerShellToken = "ps";

    /// <summary>
    ///   The configuration token for PowerShell 7.
    /// </summary>
    public const string PowerShell7Token = "ps7";

    /// <summary>
    ///   The configuration token for the Command Prompt runtime.
    /// </summary>
    public const string CommandPromptToken = "cmd";

    /// <summary>
    ///   The configuration token for Git Bash.
    /// </summary>
    public const string GitBashToken = "bash";

    /// <summary>
    ///   The runtime used when none is stored or the stored value is unrecognized.
    /// </summary>
    public const RuntimeKind Default = RuntimeKind.PowerShell;

    /// <summary>
    ///   Parses a runtime token (case-insensitive, surrounding whitespace ignored).
    ///   Returns false for null, empty, or unknown values.
    /// </summary>
    public static bool TryParse(string? value, out RuntimeKind kind)
    {
        kind = Default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "ps":
                kind = RuntimeKind.PowerShell;
                return true;
            case "ps7":
                kind = RuntimeKind.PowerShell7;
                return true;
            case "cmd":
                kind = RuntimeKind.CommandPrompt;
                return true;
            case "bash":
                kind = RuntimeKind.GitBash;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///   Parses a runtime token, falling back to <see cref="Default" />.
    /// </summary>
    public static RuntimeKind Parse(string? value) =>
        TryParse(value, out var kind) ? kind : Default;

    /// <summary>
    ///   Returns the stored token for a runtime.
    /// </summary>
    public static string ToToken(RuntimeKind kind) => kind switch
    {
        RuntimeKind.PowerShell7 => PowerShell7Token,
        RuntimeKind.CommandPrompt => CommandPromptToken,
        RuntimeKind.GitBash => GitBashToken,
        _ => PowerShellToken
    };
}
