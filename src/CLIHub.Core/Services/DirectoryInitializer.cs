namespace CLIHub.Core.Services;

/// <summary>
/// Ensures the CLIHub data directory layout exists under %APPDATA%.
/// </summary>
public static class DirectoryInitializer
{
    /// <summary>
    /// Returns the CLIHub data root: %APPDATA%\CLIHub.
    /// </summary>
    public static string GetAppDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CLIHub");

    /// <summary>
    /// Creates the data root and its standard subdirectories if they are missing.
    /// Idempotent: safe to call on every startup.
    /// </summary>
    /// <param name="root">Optional override for tests; defaults to <see cref="GetAppDataRoot"/>.</param>
    public static void EnsureAppDataLayout(string? root = null)
    {
        root ??= GetAppDataRoot();

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "logs"));
        Directory.CreateDirectory(Path.Combine(root, "plugins"));
        Directory.CreateDirectory(Path.Combine(root, "cache"));
    }
}
