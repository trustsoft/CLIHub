namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
/// Provides the user-facing release notes that ship with the application, newest version first.
/// </summary>
public interface IReleaseNotesService
{
    /// <summary>
    /// Returns every parsed release note, ordered from the newest version to the oldest.
    /// Returns an empty list when the notes are missing or unreadable.
    /// </summary>
    IReadOnlyList<ReleaseNote> GetNotes();

    /// <summary>
    /// Returns the newest release note, or null when no notes could be read.
    /// </summary>
    ReleaseNote? GetLatestNote();

    /// <summary>
    /// Returns the note for a specific version, or null when that version has no note.
    /// </summary>
    ReleaseNote? GetNote(string version);
}
