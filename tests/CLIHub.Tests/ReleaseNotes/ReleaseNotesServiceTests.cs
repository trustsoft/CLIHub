namespace CLIHub.Tests.ReleaseNotes;

using System.Reflection;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Services;

public class ReleaseNotesServiceTests
{
    private static ReleaseNotesService Create(string? text) =>
        ReleaseNotesService.CreateForTesting(NullLogger<ReleaseNotesService>.Instance, text);

    [Fact]
    public void GetNotes_MultipleVersions_OrdersNewestFirst()
    {
        var service = Create("""
        # Release Notes

        ## 0.4.0 — 2026-01-01
        ### New
        - Older

        ## 0.5.0 — 2026-02-01
        ### New
        - Newer
        """);

        var notes = service.GetNotes();

        Assert.Equal(2, notes.Count);
        Assert.Equal("0.5.0", notes[0].Version);
        Assert.Equal("0.4.0", notes[1].Version);
    }

    [Fact]
    public void GetNotes_VersionsDifferingInDigits_OrdersByVersionNotText()
    {
        var service = Create("""
        ## 0.9.0 — 2026-01-01
        ### New
        - Nine

        ## 0.10.0 — 2026-02-01
        ### New
        - Ten
        """);

        var notes = service.GetNotes();

        Assert.Equal("0.10.0", notes[0].Version);
        Assert.Equal("0.9.0", notes[1].Version);
    }

    [Fact]
    public void GetNotes_VersionWithoutAParsedOrder_KeepsItLastInDocumentOrder()
    {
        var service = Create("""
        ## 0.9.0 — 2026-01-01
        ### New
        - Nine

        ## unreleased — 2026-03-01
        ### New
        - Soon

        ## 1.0.0 — 2026-02-01
        ### New
        - One
        """);

        var versions = service.GetNotes().Select(note => note.Version).ToList();

        Assert.Equal(new[] { "1.0.0", "0.9.0", "unreleased" }, versions);
    }

    [Fact]
    public void GetNotes_SectionWithoutAGroup_ReturnsEmptyGroup()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01
        ### New
        - Added something
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal(new[] { "Added something" }, note.New);
        Assert.Empty(note.Improved);
        Assert.Empty(note.Fixed);
    }

    [Fact]
    public void GetNotes_TitleAndPreambleIgnored()
    {
        var service = Create("""
        # Release Notes

        What's new in CLIHub, newest first.

        ## 0.5.0 — 2026-02-01
        ### New
        - Added something
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal("0.5.0", note.Version);
        Assert.Equal(new[] { "Added something" }, note.New);
    }

    [Fact]
    public void GetNotes_ContentOutsideKnownGroups_IsIgnored()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01

        A paragraph that belongs to no group.

        ### New
        - Added something

        ### Migration notes
        - Should not be shown

        #### A deeper heading
        - Also not shown
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal(new[] { "Added something" }, note.New);
        Assert.Empty(note.Improved);
        Assert.Empty(note.Fixed);
    }

    [Fact]
    public void GetNotes_EntryWithMarkup_KeepsTextVerbatim()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01
        ### Improved
        - Use **bold** and `code` as written, with trailing punctuation!
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal("Use **bold** and `code` as written, with trailing punctuation!", Assert.Single(note.Improved));
    }

    [Fact]
    public void GetNotes_EntryWrappedOverTwoLines_JoinsTheContinuation()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01
        ### New
        - A long entry that
          wraps onto the next line
        - A second entry
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal(
            new[] { "A long entry that wraps onto the next line", "A second entry" },
            note.New);
    }

    [Theory]
    [InlineData("## 0.5.0 — 2026-02-01", "0.5.0", "2026-02-01")]
    [InlineData("## 0.5.0 - 2026-02-01", "0.5.0", "2026-02-01")]
    [InlineData("## 0.5.0 – 2026-02-01", "0.5.0", "2026-02-01")]
    [InlineData("## 0.6.0-beta.1 — 2026-02-01", "0.6.0-beta.1", "2026-02-01")]
    [InlineData("## 0.5.0 — 30 Sep 2026", "0.5.0", "30 Sep 2026")]
    public void GetNotes_HeadingVariants_ReadVersionAndDate(string heading, string version, string date)
    {
        var service = Create($"""
        {heading}
        ### New
        - Something
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal(version, note.Version);
        Assert.Equal(date, note.Date);
    }

    [Fact]
    public void GetNotes_LowercaseGroupName_IsAccepted()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01
        ### new
        - Added something
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal(new[] { "Added something" }, note.New);
    }

    [Fact]
    public void GetNotes_MalformedHeading_SkipsThatSectionOnly()
    {
        var service = Create("""
        ## 1.0.0
        ### New
        - Dropped with its section

        ## 0.5.0 — 2026-02-01
        ### New
        - Kept
        """);

        var note = Assert.Single(service.GetNotes());

        Assert.Equal("0.5.0", note.Version);
        Assert.Equal(new[] { "Kept" }, note.New);
    }

    [Fact]
    public void GetNotes_SeveralVersions_NewestIsFirst()
    {
        var service = Create("""
        ## 0.4.0 — 2026-01-01
        ### New
        - Older

        ## 0.5.0 — 2026-02-01
        ### New
        - Newer
        """);

        Assert.Equal("0.5.0", service.GetNotes()[0].Version);
    }

    [Fact]
    public void GetNote_KnownVersion_ReturnsThatNote()
    {
        var service = Create("""
        ## 0.4.0 — 2026-01-01
        ### New
        - Older

        ## 0.5.0 — 2026-02-01
        ### New
        - Newer
        """);

        Assert.Equal(new[] { "Older" }, service.GetNote("0.4.0")?.New);
    }

    [Fact]
    public void GetNote_UnknownVersion_ReturnsNull()
    {
        var service = Create("""
        ## 0.5.0 — 2026-02-01
        ### New
        - Newer
        """);

        Assert.Null(service.GetNote("9.9.9"));
        Assert.Null(service.GetNote("  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  \n")]
    [InlineData("# Release Notes\n\nNothing here yet.\n")]
    public void GetNotes_EmptyOrMissingDocument_ReturnsNoNotes(string? text)
    {
        var service = Create(text);

        Assert.Empty(service.GetNotes());
    }

    [Fact]
    public void GetNotes_EmbeddedDocument_ParsesAndContainsTheBuiltVersion()
    {
        var service = new ReleaseNotesService(NullLogger<ReleaseNotesService>.Instance);

        var notes = service.GetNotes();

        Assert.NotEmpty(notes);

        var builtVersion = typeof(ReleaseNotesService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        Assert.False(string.IsNullOrWhiteSpace(builtVersion));

        var normalized = builtVersion!.Split('+')[0];

        Assert.NotNull(service.GetNote(normalized));
    }
}
