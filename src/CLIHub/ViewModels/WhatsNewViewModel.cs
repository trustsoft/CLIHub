namespace CLIHub.ViewModels;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
/// View model for the What's New window: the release notes in display order, with empty groups
/// dropped so the window only shows what a version actually changed.
/// </summary>
public sealed class WhatsNewViewModel : ObservableObject
{
    /// <summary>The group headings, in the order they are displayed.</summary>
    private static readonly string[] GroupLabels = { "New", "Improved", "Fixed" };

    public WhatsNewViewModel(IReleaseNotesService releaseNotes)
    {
        Versions = BuildVersions(releaseNotes.GetNotes());
    }

    /// <summary>
    /// The release notes, newest version first.
    /// </summary>
    public IReadOnlyList<WhatsNewVersion> Versions { get; }

    /// <summary>
    /// Whether there is nothing to show, so the window displays its empty message instead.
    /// </summary>
    public bool HasNoNotes => Versions.Count == 0;

    private static IReadOnlyList<WhatsNewVersion> BuildVersions(IReadOnlyList<ReleaseNote> notes)
    {
        var versions = new List<WhatsNewVersion>(notes.Count);

        foreach (var note in notes)
        {
            versions.Add(new WhatsNewVersion(note.Version, note.Date, BuildGroups(note)));
        }

        return versions;
    }

    private static IReadOnlyList<WhatsNewGroup> BuildGroups(ReleaseNote note)
    {
        var groups = new List<WhatsNewGroup>();
        var entries = new[] { note.New, note.Improved, note.Fixed };

        for (var index = 0; index < GroupLabels.Length; index++)
        {
            if (entries[index].Count > 0)
            {
                groups.Add(new WhatsNewGroup(GroupLabels[index], entries[index]));
            }
        }

        return groups;
    }
}
