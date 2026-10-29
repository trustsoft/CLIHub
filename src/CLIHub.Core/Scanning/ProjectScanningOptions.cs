namespace CLIHub.Core.Scanning;

using CLIHub.Core.Models;

/// <summary>
///   Effective settings for a repository scan pass.
/// </summary>
/// <param name="WatchedDirectories"> Root directories to scan. </param>
/// <param name="ScanDepth"> Levels below each root that are inspected when the root has no override. </param>
/// <param name="IgnoredPatterns"> Directory-name patterns skipped during the walk. </param>
/// <param name="DiscoveryMode"> Which directories count as repositories when the root has no override. </param>
/// <param name="WatchedDirectorySettings">
///   Per-root overrides keyed by watched root path. A root without an entry uses
///   <paramref name="ScanDepth" /> and <paramref name="DiscoveryMode" />.
/// </param>
public sealed record ProjectScanningOptions(IReadOnlyList<string> WatchedDirectories,
                                            int ScanDepth,
                                            IReadOnlyList<string> IgnoredPatterns,
                                            ProjectDiscoveryMode DiscoveryMode,
                                            IReadOnlyDictionary<string, WatchedDirectoryScanSettings>? WatchedDirectorySettings = null)
{
    /// <summary>
    ///   Creates effective options from stored preferences, applying the scanning defaults.
    /// </summary>
    /// <param name="scanning"> The stored scanning preferences. </param>
    /// <returns> The effective scanning options. </returns>
    public static ProjectScanningOptions From(ScanningPreferences scanning)
    {
        ArgumentNullException.ThrowIfNull(scanning);

        return new ProjectScanningOptions(
            scanning.WatchedDirectories is { Count: > 0 } watched ? watched : Array.Empty<string>(),
            ProjectScanningDefaults.NormalizeScanDepth(scanning.ScanDepth),
            scanning.IgnoredPatterns ?? ProjectScanningDefaults.IgnoredPatterns,
            scanning.DiscoveryMode,
            scanning.WatchedDirectorySettings);
    }

    /// <summary>
    ///   Returns the effective options for one watched root: the root's stored depth and discovery
    ///   mode when present, otherwise this pass's defaults.
    /// </summary>
    /// <param name="root"> The watched root. </param>
    /// <returns> The options to use while walking the root. </returns>
    public ProjectScanningOptions ForRoot(string root)
    {
        if (this.WatchedDirectorySettings is null || string.IsNullOrWhiteSpace(root))
        {
            return this;
        }

        var settings = this.WatchedDirectorySettings
            .FirstOrDefault(entry => string.Equals(entry.Key, root, StringComparison.OrdinalIgnoreCase))
            .Value;

        return settings is null
            ? this
            : this with
            {
                ScanDepth = ProjectScanningDefaults.NormalizeScanDepth(settings.ScanDepth),
                DiscoveryMode = settings.DiscoveryMode
            };
    }
}
