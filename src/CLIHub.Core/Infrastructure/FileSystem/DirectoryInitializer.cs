namespace CLIHub.Core.Infrastructure.FileSystem;

/// <summary>
///   Ensures the CLIHub data directory layout exists under <see cref="AppPaths.Root"/>.
/// </summary>
public static class DirectoryInitializer
{
    /// <summary>
    ///   Creates the data root and its standard subdirectories if they are missing.
    ///   Idempotent: safe to call on every startup.
    /// </summary>
    /// <param name="root"> Optional override for tests; defaults to <see cref="AppPaths.Root"/>. </param>
    public static void EnsureAppDataLayout(string? root = null)
    {
        root ??= AppPaths.Root;

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, AppPaths.LogsDirectoryName));
        Directory.CreateDirectory(Path.Combine(root, AppPaths.PluginsDirectoryName));
        Directory.CreateDirectory(Path.Combine(root, AppPaths.CacheDirectoryName));
    }
}
