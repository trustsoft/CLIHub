namespace CLIHub.Core.Services;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
///   Reads the user-facing release notes embedded in this assembly and parses them into
///   <see cref="ReleaseNote"/> records, ordered from the newest version to the oldest.
/// </summary>
/// <remarks>
///   The parser understands one shape only: a version section heading
///   (<c>## &lt;version&gt; — &lt;date&gt;</c>, with an em dash, an en dash, or a hyphen as the separator)
///   followed by the groups <c>### New</c>, <c>### Improved</c>, and <c>### Fixed</c> whose entries are
///   list items. Titles, preambles, prose outside those groups, and prose under an unknown group are
///   ignored. Entry text is kept as written.
/// </remarks>
public class ReleaseNotesService : IReleaseNotesService
{
    /// <summary>
    ///   Logical name of the embedded notes document, set by the project file.
    /// </summary>
    internal const string ResourceName = "CLIHub.Core.ReleaseNotes.RELEASE-NOTES.md";

    private const string ResourceSuffix = "RELEASE-NOTES.md";
    private const string VersionMarker = "## ";
    private const string GroupMarker = "### ";
    private const string EmDash = "\u2014";
    private const string EnDash = "\u2013";
    private const string SpacedHyphen = " - ";

    private const string NewGroup = "New";
    private const string ImprovedGroup = "Improved";
    private const string FixedGroup = "Fixed";

    private readonly ILogger<ReleaseNotesService> _logger;
    private readonly Lazy<IReadOnlyList<ReleaseNote>> _notes;

    /// <summary>
    ///   Creates the service, reading the notes embedded in this assembly.
    /// </summary>
    public ReleaseNotesService(ILogger<ReleaseNotesService> logger)
        : this(logger, () => ReadEmbeddedNotes(logger))
    {
    }

    /// <summary>
    ///   Test seam: parses caller-provided note text instead of the embedded document.
    /// </summary>
    internal ReleaseNotesService(ILogger<ReleaseNotesService> logger, string? noteText)
        : this(logger, () => noteText)
    {
    }

    private ReleaseNotesService(ILogger<ReleaseNotesService> logger, Func<string?> readNotes)
    {
        _logger = logger;
        _notes = new Lazy<IReadOnlyList<ReleaseNote>>(() => Parse(readNotes()));
    }

    /// <inheritdoc />
    public IReadOnlyList<ReleaseNote> GetNotes() => _notes.Value;

    /// <inheritdoc />
    public ReleaseNote? GetNote(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var wanted = version.Trim();
        var notes = GetNotes();

        var exact = notes.FirstOrDefault(note =>
            string.Equals(note.Version, wanted, StringComparison.OrdinalIgnoreCase));

        if (exact != null)
        {
            return exact;
        }

        // A parsed fallback, so versions that differ only in formatting still match.
        return Version.TryParse(wanted, out var parsed)
            ? notes.FirstOrDefault(note =>
                Version.TryParse(note.Version, out var candidate) && candidate == parsed)
            : null;
    }

    private IReadOnlyList<ReleaseNote> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Release notes are empty or missing; no notes will be shown");
            return Array.Empty<ReleaseNote>();
        }

        var notes = new List<ReleaseNote>();
        PendingNote? pending = null;
        List<string>? group = null;
        var lineNumber = 0;

        foreach (var rawLine in text.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');

            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith(GroupMarker, StringComparison.Ordinal))
            {
                group = pending?.GroupFor(line[GroupMarker.Length..].Trim());
                continue;
            }

            if (line.StartsWith(VersionMarker, StringComparison.Ordinal))
            {
                if (pending != null)
                {
                    notes.Add(pending.ToNote());
                }

                pending = StartNote(line[VersionMarker.Length..].Trim(), lineNumber);
                group = null;
                continue;
            }

            if (pending == null)
            {
                // Title or preamble before the first version section.
                continue;
            }

            if (TryReadEntry(line, out var entry))
            {
                group?.Add(entry);
                continue;
            }

            if (line[0] == '#')
            {
                // A deeper heading of an unknown kind.
                continue;
            }

            // A hand-wrapped continuation of the previous entry.
            if (group is { Count: > 0 })
            {
                group[^1] = group[^1] + " " + line.Trim();
            }
        }

        if (pending != null)
        {
            notes.Add(pending.ToNote());
        }

        var ordered = OrderByVersion(notes);
        _logger.LogInformation("Read {Count} release note(s)", ordered.Count);
        return ordered;
    }

    private PendingNote? StartNote(string heading, int lineNumber)
    {
        if (TrySplitHeading(heading, out var version, out var date))
        {
            return new PendingNote(version, date);
        }

        _logger.LogWarning(
            "Skipped the release notes section at line {Line}: '{Heading}' is not a '<version> — <date>' heading",
            lineNumber,
            heading);
        return null;
    }

    /// <summary>
    ///   Splits a section heading into its version and date. The documented separator is an em dash;
    ///   an en dash or a spaced hyphen is accepted as well so a hand-edited file still parses. A
    ///   version stays in one piece, so a prerelease such as "0.6.0-beta.1" is not split at its hyphen.
    /// </summary>
    private static bool TrySplitHeading(string heading, out string version, out string date)
    {
        version = string.Empty;
        date = string.Empty;

        foreach (var separator in new[] { EmDash, EnDash, SpacedHyphen })
        {
            var index = heading.IndexOf(separator, StringComparison.Ordinal);

            if (index < 0)
            {
                continue;
            }

            var candidateVersion = heading[..index].Trim();
            var candidateDate = heading[(index + separator.Length)..].Trim();

            if (candidateVersion.Length > 0 && candidateDate.Length > 0)
            {
                version = candidateVersion;
                date = candidateDate;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadEntry(string line, out string entry)
    {
        entry = string.Empty;
        var trimmed = line.TrimStart();

        if (trimmed.Length < 2 || (trimmed[0] != '-' && trimmed[0] != '*') || trimmed[1] != ' ')
        {
            return false;
        }

        entry = trimmed[2..].Trim();
        return entry.Length > 0;
    }

    /// <summary>
    ///   Orders notes by version descending. A version that does not parse as a semantic version
    ///   keeps its document position at the end of the list instead of breaking the ordering.
    /// </summary>
    private static List<ReleaseNote> OrderByVersion(List<ReleaseNote> notes) =>
        notes
            .Select((note, index) => (Note: note, Index: index, Parsed: ParseVersion(note.Version)))
            .OrderBy(item => item.Parsed == null)
            .ThenByDescending(item => item.Parsed ?? new Version(0, 0))
            .ThenBy(item => item.Index)
            .Select(item => item.Note)
            .ToList();

    private static Version? ParseVersion(string version) =>
        Version.TryParse(version.TrimStart('v', 'V'), out var parsed) ? parsed : null;

    private static string? ReadEmbeddedNotes(ILogger logger)
    {
        try
        {
            var assembly = typeof(ReleaseNotesService).Assembly;

            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate, ResourceName, StringComparison.Ordinal) ||
                    candidate.EndsWith(ResourceSuffix, StringComparison.Ordinal));

            if (name == null)
            {
                logger.LogWarning("No embedded release notes resource was found in the assembly");
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(name);

            if (stream == null)
            {
                logger.LogWarning("Embedded release notes resource {Resource} could not be opened", name);
                return null;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Embedded release notes could not be read");
            return null;
        }
    }

    /// <summary>
    ///   A version section while it is being read from the document.
    /// </summary>
    private sealed class PendingNote
    {
        private readonly List<string> _new = new();
        private readonly List<string> _improved = new();
        private readonly List<string> _fixed = new();

        public PendingNote(string version, string date)
        {
            Version = version;
            Date = date;
        }

        public string Version { get; }

        public string Date { get; }

        /// <summary>
        ///   Returns the list that a group heading collects into, or null for a group the
        ///   application does not show.
        /// </summary>
        public List<string>? GroupFor(string name)
        {
            if (name.Equals(NewGroup, StringComparison.OrdinalIgnoreCase))
            {
                return _new;
            }

            if (name.Equals(ImprovedGroup, StringComparison.OrdinalIgnoreCase))
            {
                return _improved;
            }

            return name.Equals(FixedGroup, StringComparison.OrdinalIgnoreCase) ? _fixed : null;
        }

        public ReleaseNote ToNote() => new(Version, Date, _new, _improved, _fixed);
    }
}
