namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Coordinates the one-time release-notes decision made during application startup.
/// </summary>
public interface IReleaseNotesStartupCoordinator
{
    /// <summary>
    ///   Evaluates the current version and performs the required release-notes action.
    /// </summary>
    /// <param name="preferences"> The preferences loaded for this application start. </param>
    void Evaluate(AppPreferences preferences);
}
