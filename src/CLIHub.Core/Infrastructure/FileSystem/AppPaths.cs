namespace CLIHub.Core.Infrastructure.FileSystem;

/// <summary>
///   The CLIHub data layout under <c>%APPDATA%\CLIHub</c>, as documented in
///   <c>docs/architecture.md → File System Layout</c>: the single place where the folder and
///   file names are written down.
/// </summary>
public static class AppPaths
{
    /// <summary>
    ///   The name of the logs subdirectory under the data root.
    /// </summary>
    public const string LogsDirectoryName = "logs";

    /// <summary>
    ///   The name of the plugins subdirectory under the data root.
    /// </summary>
    public const string PluginsDirectoryName = "plugins";

    /// <summary>
    ///   The name of the cache subdirectory under the data root.
    /// </summary>
    public const string CacheDirectoryName = "cache";

    /// <summary>
    ///   The CLIHub data root: <c>%APPDATA%\CLIHub</c>.
    /// </summary>
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CLIHub");

    /// <summary>
    ///   The directory holding the rolling log files: <c>%APPDATA%\CLIHub\logs</c>.
    /// </summary>
    public static string LogsDirectory { get; } = Path.Combine(Root, LogsDirectoryName);

    /// <summary>
    ///   The directory holding agent plugin folders: <c>%APPDATA%\CLIHub\plugins</c>.
    /// </summary>
    public static string PluginsDirectory { get; } = Path.Combine(Root, PluginsDirectoryName);

    /// <summary>
    ///   The directory holding disposable cache state: <c>%APPDATA%\CLIHub\cache</c>.
    /// </summary>
    public static string CacheDirectory { get; } = Path.Combine(Root, CacheDirectoryName);

    /// <summary>
    ///   The application configuration file: <c>%APPDATA%\CLIHub\config.json</c>.
    /// </summary>
    public static string ConfigFile { get; } = Path.Combine(Root, "config.json");

    /// <summary>
    ///   The persistent logo cache state file: <c>%APPDATA%\CLIHub\cache\logos.json</c>.
    /// </summary>
    public static string LogosCacheFile { get; } = Path.Combine(CacheDirectory, "logos.json");
}
