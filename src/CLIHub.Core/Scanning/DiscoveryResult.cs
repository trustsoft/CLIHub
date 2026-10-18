namespace CLIHub.Core.Scanning;

/// <summary>
///   Outcome of a single scan pass.
/// </summary>
/// <param name="DiscoveredPaths"> Normalized paths of the repositories discovered this pass. </param>
/// <param name="ScannedRoots">
///   Normalized paths of the watched roots that were enumerated successfully this pass.
///   Roots that were missing or unreadable are absent, so projects under them are not treated as removed.
/// </param>
/// <param name="ScannedDirectoryCounts">
///   Number of directories the walk examined under each successfully scanned root, keyed by normalized root path.
/// </param>
public sealed record DiscoveryResult(IReadOnlyList<string> DiscoveredPaths,
                                     IReadOnlyList<string> ScannedRoots,
                                     IReadOnlyDictionary<string, int> ScannedDirectoryCounts)
{
    /// <summary>
    ///   The number of directories the walk examined across every successfully scanned root.
    /// </summary>
    public int ScannedDirectoryCount => this.ScannedDirectoryCounts.Values.Sum();
}
