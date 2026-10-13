namespace CLIHub.Core.Models;

/// <summary>
///   What the application should do about the <c>What's New</c> window at startup.
/// </summary>
public enum ReleaseNotesPromptAction
{
    /// <summary>
    ///   Do nothing: the notes were already shown, or this is a first run.
    /// </summary>
    Skip,

    /// <summary>
    ///   Show the window once and record the running version.
    /// </summary>
    Show,

    /// <summary>
    ///   Show nothing, but record the running version so the check does not repeat.
    /// </summary>
    RecordOnly
}

/// <summary>
///   Decides whether the release notes should be shown on this start, from the recorded version,
///   the running version, and whether notes exist for the running version.
/// </summary>
public static class ReleaseNotesPrompt
{
    /// <summary>
    ///   Returns what to do at startup:
    ///   <see cref="ReleaseNotesPromptAction.Show"/> when the version changed and it has notes,
    ///   <see cref="ReleaseNotesPromptAction.RecordOnly"/> when the version changed but has no notes —
    ///   or when nothing has been recorded yet, so a first run remembers the shipped version without
    ///   announcing it — and <see cref="ReleaseNotesPromptAction.Skip"/> when nothing is to be done.
    /// </summary>
    /// <param name="recordedVersion"> The last version whose notes were shown, or null on a first run. </param>
    /// <param name="currentVersion"> The running application version. </param>
    /// <param name="hasNotesForCurrentVersion"> Whether the running version has release notes. </param>
    public static ReleaseNotesPromptAction Decide(
        string? recordedVersion,
        string currentVersion,
        bool hasNotesForCurrentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            // An unknown running version must never overwrite the recorded one.
            return ReleaseNotesPromptAction.Skip;
        }

        if (string.IsNullOrWhiteSpace(recordedVersion))
        {
            // First run: the window is not opened for a version the user did not upgrade from, but
            // the shipped version is remembered, so the next upgrade is announced.
            return ReleaseNotesPromptAction.RecordOnly;
        }

        if (string.Equals(recordedVersion.Trim(), currentVersion.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ReleaseNotesPromptAction.Skip;
        }

        return hasNotesForCurrentVersion
            ? ReleaseNotesPromptAction.Show
            : ReleaseNotesPromptAction.RecordOnly;
    }
}
