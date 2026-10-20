namespace CLIHub.Core.Infrastructure.Processes;

using System.Text;

using CLIHub.Core.Models;

/// <summary>
///   Builds runtime-specific Windows command lines from plugin command descriptors.
/// </summary>
internal static class WindowsCommandLineBuilder
{
    /// <summary>
    ///   Builds the executable and arguments used to launch an interactive command.
    /// </summary>
    /// <param name="runtime"> The selected interactive runtime. </param>
    /// <param name="command"> The plugin command. </param>
    /// <param name="workingDirectory"> The command working directory. </param>
    /// <param name="gitBashExecutable">
    ///   The absolute path of the resolved Git for Windows <c> bash.exe </c>; required for
    ///   <see cref="RuntimeKind.GitBash" /> and ignored by the other runtimes.
    /// </param>
    /// <param name="powerShell7Executable">
    ///   The absolute path of the resolved PowerShell 7 <c> pwsh.exe </c>; required for
    ///   <see cref="RuntimeKind.PowerShell7" /> and ignored by the other runtimes.
    /// </param>
    /// <returns> The process executable and argument string. </returns>
    internal static (string FileName, string Arguments) BuildInteractive(RuntimeKind runtime,
                                                                         PluginCommand command,
                                                                         string workingDirectory,
                                                                         string? gitBashExecutable,
                                                                         string? powerShell7Executable)
    {
        var commandLine = BuildCommandLine(command);

        return runtime switch
        {
            RuntimeKind.CommandPrompt => ("cmd.exe", $"/d /k {QuoteCmdCommand(commandLine)}"),
            RuntimeKind.PowerShell7 => (powerShell7Executable ?? throw new InvalidOperationException(
                "PowerShell 7 executable was not resolved"), $"-NoExit -EncodedCommand {EncodePowerShell(commandLine)}"),
            RuntimeKind.GitBash => (gitBashExecutable ?? throw new InvalidOperationException(
                "Git Bash executable was not resolved"), $"-lic {QuoteBashCommand(commandLine)}"),
            _ => ("powershell.exe", $"-NoExit -EncodedCommand {EncodePowerShell(commandLine)}")
        };
    }

    /// <summary>
    ///   Builds the command interpreter arguments used to capture output from an executable.
    /// </summary>
    /// <param name="executable"> The executable or PATH-resolved shim. </param>
    /// <param name="arguments"> The raw plugin argument tail. </param>
    /// <returns> The command interpreter executable and arguments. </returns>
    internal static (string FileName, string Arguments) BuildCapturedCommand(string executable,
                                                                             string? arguments)
    {
        var commandLine = BuildCommandLine(executable, arguments);
        return (Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", $"/d /s /c {QuoteCmdCommand(commandLine)}");
    }

    /// <summary>
    ///   Builds an executable command line while preserving the descriptor's raw argument tail.
    /// </summary>
    /// <param name="command"> The plugin command. </param>
    /// <returns> The command line. </returns>
    internal static string BuildCommandLine(PluginCommand command) =>
        BuildCommandLine(command.Executable, command.Arguments);

    private static string BuildCommandLine(string? executable, string? arguments)
    {
        var executablePart = QuoteWindowsArgument(string.IsNullOrWhiteSpace(executable) ? "cmd" : executable);
        var argumentPart = EscapeCmdArgumentTail(arguments);
        return string.IsNullOrWhiteSpace(argumentPart)
            ? executablePart
            : $"{executablePart} {argumentPart}";
    }

    private static string QuoteCmdCommand(string commandLine) =>
        $"\"{EscapeCmdArgumentTail(commandLine)}\"";

    private static string EscapeCmdArgumentTail(string? arguments, bool escapeQuotes = false)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(arguments.Length + 16);
        foreach (var character in arguments)
        {
            if (character is '&' or '|' or '<' or '>' or '(' or ')' or '^')
            {
                builder.Append('^');
            }
            else if (character == '%')
            {
                builder.Append('%');
            }

            if (escapeQuotes && character == '"')
            {
                builder.Append('^');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string QuoteWindowsArgument(string value)
    {
        if (value.Length > 0 && !value.Any(char.IsWhiteSpace) && !value.Contains('"'))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashes * 2 + 1);
                builder.Append('"');
                backslashes = 0;
                continue;
            }

            builder.Append('\\', backslashes);
            backslashes = 0;
            builder.Append(character);
        }

        builder.Append('\\', backslashes * 2);
        builder.Append('"');
        return builder.ToString();
    }

    private static string QuoteBashCommand(string commandLine) =>
        $"\"{EscapeBashArgumentTail(commandLine)}\"";

    private static string EscapeBashArgumentTail(string arguments)
    {
        var builder = new StringBuilder(arguments.Length + 8);
        foreach (var character in arguments)
        {
            // In a bash double-quoted string these characters are special; a leading backslash
            // keeps them literal while leaving ordinary path separators readable.
            if (character is '"' or '`' or '$' or '\\')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string EncodePowerShell(string commandLine) =>
        Convert.ToBase64String(Encoding.Unicode.GetBytes(QuotePowerShellExecutable(commandLine)));

    private static string QuotePowerShellExecutable(string commandLine)
    {
        var executableEnd = commandLine.StartsWith('"')
            ? commandLine.IndexOf('"', 1)
            : commandLine.IndexOf(' ');

        if (executableEnd < 0)
        {
            return $"& '{commandLine.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        var executableStart = commandLine.StartsWith('"') ? 1 : 0;
        var executable = commandLine.Substring(executableStart, executableEnd - executableStart);
        var arguments = commandLine[(executableEnd + 1)..].TrimStart();

        if (string.IsNullOrEmpty(arguments))
        {
            return $"& '{executable.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        // With arguments the invocation must target a native application: the stop-parsing
        // token is honored only for native executables, so a PATH name that resolves to a
        // PowerShell script shim (.ps1) would otherwise receive the token literally and
        // reject it as an unknown flag.
        if (executable.Contains('\\') || executable.Contains('/'))
        {
            var escaped = executable.Replace("'", "''", StringComparison.Ordinal);
            return EndsWithNativeExecutableExtension(executable)
                ? $"& '{escaped}' --% {arguments}"
                : $"$exe = (Get-Command '{EscapePsWildcard(executable)}' -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source\n& $exe --% {arguments}";
        }

        return $"$exe = (Get-Command '{executable.Replace("'", "''", StringComparison.Ordinal)}' -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source\n& $exe --% {arguments}";
    }

    private static bool EndsWithNativeExecutableExtension(string executable) =>
        executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
        executable.EndsWith(".com", StringComparison.OrdinalIgnoreCase);

    // Get-Command -Name treats its argument as a wildcard pattern; backtick is its escape.
    private static string EscapePsWildcard(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            if (character is '*' or '?' or '[' or ']' or '`')
            {
                builder.Append('`');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
