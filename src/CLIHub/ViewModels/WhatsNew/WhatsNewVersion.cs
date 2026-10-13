namespace CLIHub.ViewModels;

/// <summary>
///   One version's release notes as the What's New window displays them.
/// </summary>
/// <param name="Version"> The released version. </param>
/// <param name="Date"> The release date, as written in the notes document. </param>
/// <param name="Groups"> The non-empty groups for this version, in display order. </param>
public sealed record WhatsNewVersion(string Version, string Date, IReadOnlyList<WhatsNewGroup> Groups);

/// <summary>
///   One non-empty group of entries for a version.
/// </summary>
/// <param name="Label"> The group heading, for example <c>New</c>. </param>
/// <param name="Entries"> The entries, in document order. </param>
public sealed record WhatsNewGroup(string Label, IReadOnlyList<string> Entries);
