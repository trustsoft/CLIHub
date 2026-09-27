namespace CLIHub.Core.Models;

/// <summary>
/// Outcome of an update check.
/// </summary>
public enum UpdateStatus
{
    /// <summary>The installed version is current.</summary>
    UpToDate,

    /// <summary>A newer version is available.</summary>
    UpdateAvailable,

    /// <summary>The app is not a Velopack install, so updates cannot be checked.</summary>
    NotInstalled,

    /// <summary>The check failed or timed out.</summary>
    Failed
}

/// <summary>
/// Result of checking for updates.
/// </summary>
/// <param name="Status">The check outcome.</param>
/// <param name="CurrentVersion">The current application version.</param>
/// <param name="AvailableVersion">The available version, when <see cref="UpdateStatus.UpdateAvailable"/>.</param>
public sealed record UpdateCheckResult(UpdateStatus Status, string CurrentVersion, string? AvailableVersion);
