using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CLIHub.Core.Logging;

/// <summary>
/// Configures the Serilog file-logging pipeline.
/// </summary>
public static class LoggingSetup
{
    /// <summary>
    /// Creates a file logger writing daily-rotated logs into <paramref name="logsDirectory"/>.
    /// </summary>
    /// <param name="logsDirectory">Directory that receives the log files.</param>
    /// <param name="minimumLevel">Minimum level to record.</param>
    public static Logger CreateLogger(string logsDirectory, LogEventLevel minimumLevel)
    {
        Directory.CreateDirectory(logsDirectory);

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(
                path: Path.Combine(logsDirectory, "clihub-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
