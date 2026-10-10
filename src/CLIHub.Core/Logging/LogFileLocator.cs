namespace CLIHub.Core.Logging;

/// <summary>
///   Resolves the log file paths used by the file sink.
/// </summary>
public static class LogFileLocator
{
    private const string FileNamePrefix = "clihub-";

    /// <summary>
    ///   Returns the current daily log file path for the given logs directory.
    /// </summary>
    /// <param name="logsDirectory"> The logs directory. </param>
    /// <param name="now"> The time to use, or the current local time when null. </param>
    /// <returns> The full path of the current daily log file. </returns>
    public static string CurrentLogFilePath(string logsDirectory, DateTime? now = null) =>
        Path.Combine(logsDirectory, $"{FileNamePrefix}{(now ?? DateTime.Now):yyyyMMdd}.log");
}
