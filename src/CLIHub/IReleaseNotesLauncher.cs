namespace CLIHub;

/// <summary>
///   The single place through which the user-facing release notes are presented: it owns the
///   What's New window and records which version's notes the user has seen.
/// </summary>
public interface IReleaseNotesLauncher
{
    /// <summary>
    ///   Opens the What's New window (or brings the open one to the front) and records the running
    ///   version as seen.
    /// </summary>
    void ShowReleaseNotes();

    /// <summary>
    ///   Records the running version as seen without opening the window, so a version that has no
    ///   notes does not trigger the check again on every start.
    /// </summary>
    void MarkReleaseNotesSeen();
}
