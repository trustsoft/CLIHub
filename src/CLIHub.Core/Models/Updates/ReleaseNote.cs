namespace CLIHub.Core.Models;

/// <summary>
///   One version's user-facing release notes, as written in <c>RELEASE-NOTES.md</c>.
/// </summary>
/// <param name="Version"> The released version, for example <c>0.5.0</c>. </param>
/// <param name="Date"> The release date, exactly as written in the document. </param>
/// <param name="New"> The "New" entries, in document order. </param>
/// <param name="Improved"> The "Improved" entries, in document order. </param>
/// <param name="Fixed"> The "Fixed" entries, in document order. </param>
public sealed record ReleaseNote(
    string Version,
    string Date,
    IReadOnlyList<string> New,
    IReadOnlyList<string> Improved,
    IReadOnlyList<string> Fixed);
