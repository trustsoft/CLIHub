namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Immutable state snapshot loaded during application startup.
/// </summary>
/// <param name="Preferences"> The loaded user preferences. </param>
public sealed record StartupState(AppPreferences Preferences);
