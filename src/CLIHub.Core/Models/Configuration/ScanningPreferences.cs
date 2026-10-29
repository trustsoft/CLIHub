namespace CLIHub.Core.Models;

/// <summary>
///   User preferences for repository discovery scanning.
/// </summary>
public sealed class ScanningPreferences
{
    /// <summary>
    ///   Root directories watched for repository discovery. Null or empty means nothing is watched.
    /// </summary>
    public List<string>? WatchedDirectories { get; set; }

    /// <summary>
    ///   Per-directory scanning overrides keyed by normalized watched root path.
    /// </summary>
    public IReadOnlyDictionary<string, WatchedDirectoryScanSettings> WatchedDirectorySettings { get; set; } =
        new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///   How many directory levels below each watched root are scanned. Defaults to 2.
    /// </summary>
    public int ScanDepth { get; set; } = ProjectScanningDefaults.DefaultScanDepth;

    /// <summary>
    ///   Directory-name patterns skipped during scanning. Null means the built-in defaults are used.
    /// </summary>
    public List<string>? IgnoredPatterns { get; set; }

    /// <summary>
    ///   Which directories count as repositories during discovery. Defaults to both agents and Git repositories.
    /// </summary>
    public ProjectDiscoveryMode DiscoveryMode { get; set; } = ProjectDiscoveryModes.Default;

    /// <summary>
    ///   Creates a detached copy of these scanning preferences.
    /// </summary>
    /// <returns> A detached scanning preferences copy. </returns>
    public ScanningPreferences Copy() => new()
    {
        WatchedDirectories = this.WatchedDirectories is null ? null : new List<string>(this.WatchedDirectories),
        WatchedDirectorySettings = this.WatchedDirectorySettings is null
            ? new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, WatchedDirectoryScanSettings>(
                this.WatchedDirectorySettings,
                StringComparer.OrdinalIgnoreCase),
        ScanDepth = this.ScanDepth,
        IgnoredPatterns = this.IgnoredPatterns is null ? null : new List<string>(this.IgnoredPatterns),
        DiscoveryMode = this.DiscoveryMode
    };
}
