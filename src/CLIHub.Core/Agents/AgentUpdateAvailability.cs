namespace CLIHub.Core.Agents;

/// <summary>
///   Outcome of an agent update availability check.
/// </summary>
public enum AgentUpdateStatus
{
    /// <summary>
    ///   Availability could not be determined: no source declared, an unusable source, a failed
    ///   check, or an unparseable version on either side.
    /// </summary>
    Unknown,

    /// <summary>
    ///   The installed version is equal to or newer than the latest published version.
    /// </summary>
    UpToDate,

    /// <summary>
    ///   The agent's distribution source publishes a newer version than the installed one.
    /// </summary>
    UpdateAvailable
}

/// <summary>
///   Result of an agent update availability check. The result is advisory: it never gates or
///   alters the Update action.
/// </summary>
/// <param name="Status"> The outcome. </param>
/// <param name="InstalledVersion"> The extracted installed version, for example "2.0.32", or null when unknown. </param>
/// <param name="LatestVersion"> The extracted latest published version, or null when unknown. </param>
public sealed record AgentUpdateAvailability(
    AgentUpdateStatus Status,
    string? InstalledVersion = null,
    string? LatestVersion = null);
