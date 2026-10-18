namespace CLIHub.Core.Models;

/// <summary>
///   Per-directory scanning overrides for one watched root.
/// </summary>
/// <param name="ScanDepth"> Levels below the root that are inspected. </param>
/// <param name="DiscoveryMode"> Which directories beneath the root count as repositories. </param>
public sealed record WatchedDirectoryScanSettings(int ScanDepth, ProjectDiscoveryMode DiscoveryMode);
