namespace CLIHub.Core.Logging;

using Serilog.Events;

/// <summary>
///   Maps configured level names to <see cref="LogEventLevel"/>.
/// </summary>
public static class LogLevelParser
{
    /// <summary>
    ///   Parses a level name (case-insensitive). Unknown or null values yield Information.
    /// </summary>
    public static LogEventLevel Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" or "critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
}
