namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   Owns the user-facing update-check outcome text shared by the update surfaces, so the display
///   literals live in exactly one place.
/// </summary>
internal static class UpdateOutcomeText
{
    /// <summary>
    ///   Returns the outcome message for an update-check result.
    /// </summary>
    /// <param name="result"> The completed update-check result. </param>
    /// <returns> The user-facing outcome message. </returns>
    internal static string ForCheck(UpdateCheckResult result) =>
        result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };
}
