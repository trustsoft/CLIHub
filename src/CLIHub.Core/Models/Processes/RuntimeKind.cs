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
///   Maps <see cref="RuntimeKind" /> to and from its stored token and resolves legacy
///   terminal-executable values.
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
    ///   Parses a runtime token (or a recognized executable/alias). Returns false for
    ///   null, empty, or unknown values.
    /// </summary>
    public static bool TryParse(string? value, out RuntimeKind kind)
    {
        kind = RuntimeKind.PowerShell;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "ps":
            case "powershell":
            case "powershell.exe":
                kind = RuntimeKind.PowerShell;
                return true;
            case "ps7":
            case "pwsh":
            case "pwsh.exe":
            case "powershell7":
            case "powershell-7":
            case "powershell 7":
                kind = RuntimeKind.PowerShell7;
                return true;
            case "cmd":
            case "cmd.exe":
            case "command prompt":
            case "commandprompt":
                kind = RuntimeKind.CommandPrompt;
                return true;
            case "bash":
            case "bash.exe":
            case "gitbash":
            case "git-bash":
            case "git bash":
                kind = RuntimeKind.GitBash;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///   Parses a runtime token, falling back to <see cref="RuntimeKind.PowerShell" />.
    /// </summary>
    public static RuntimeKind Parse(string? value) =>
        TryParse(value, out var kind) ? kind : RuntimeKind.PowerShell;

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
