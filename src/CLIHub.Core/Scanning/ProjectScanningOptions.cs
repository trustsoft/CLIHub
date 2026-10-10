namespace CLIHub.Core.Scanning;

using CLIHub.Core.Models;

/// <summary>
///   Effective settings for a repository scan pass.
/// </summary>
/// <param name="WatchedDirectories"> Root directories to scan. </param>
/// <param name="ScanDepth"> Levels below each root that are inspected. </param>
/// <param name="IgnoredPatterns"> Directory-name patterns skipped during the walk. </param>
/// <param name="DiscoveryMode"> Which directories count as repositories. </param>
public sealed record ProjectScanningOptions(
    IReadOnlyList<string> WatchedDirectories,
    int ScanDepth,
    IReadOnlyList<string> IgnoredPatterns,
    ProjectDiscoveryMode DiscoveryMode)
{
    /// <summary>
    ///   Creates effective options from stored preferences, applying the scanning defaults.
    /// </summary>
    /// <param name="preferences"> The stored preferences. </param>
    /// <returns> The effective scanning options. </returns>
    public static ProjectScanningOptions From(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return new ProjectScanningOptions(
            preferences.WatchedDirectories is { Count: > 0 } watched ? watched : Array.Empty<string>(),
            ProjectScanningDefaults.NormalizeScanDepth(preferences.ScanDepth),
            preferences.IgnoredPatterns ?? ProjectScanningDefaults.IgnoredPatterns,
            preferences.DiscoveryMode);
    }
}
