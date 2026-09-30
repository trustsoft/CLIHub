namespace CLIHub.Tests.ReleaseNotes;

using CLIHub.Core.Models;

public class ReleaseNotesPromptTests
{
    [Fact]
    public void Decide_VersionChangedAndNotesExist_ShowsOnce()
    {
        var action = ReleaseNotesPrompt.Decide("0.4.0", "0.5.0", hasNotesForCurrentVersion: true);

        Assert.Equal(ReleaseNotesPromptAction.Show, action);
    }

    [Fact]
    public void Decide_VersionChangedWithoutNotes_RecordsOnly()
    {
        var action = ReleaseNotesPrompt.Decide("0.4.0", "0.5.0", hasNotesForCurrentVersion: false);

        Assert.Equal(ReleaseNotesPromptAction.RecordOnly, action);
    }

    [Fact]
    public void Decide_SameVersion_Skips()
    {
        var action = ReleaseNotesPrompt.Decide("0.5.0", "0.5.0", hasNotesForCurrentVersion: true);

        Assert.Equal(ReleaseNotesPromptAction.Skip, action);
    }

    [Theory]
    [InlineData("0.5.0", " 0.5.0 ")]
    [InlineData("0.5.0", "0.5.0")]
    public void Decide_SameVersionIgnoringPaddingAndCase_Skips(string recorded, string current)
    {
        Assert.Equal(ReleaseNotesPromptAction.Skip, ReleaseNotesPrompt.Decide(recorded, current, true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_NothingRecorded_RecordsTheShippedVersionWithoutShowing(string? recorded)
    {
        var action = ReleaseNotesPrompt.Decide(recorded, "0.5.0", hasNotesForCurrentVersion: true);

        Assert.Equal(ReleaseNotesPromptAction.RecordOnly, action);
    }

    [Fact]
    public void Decide_DowngradeWithNotes_Shows()
    {
        var action = ReleaseNotesPrompt.Decide("0.6.0", "0.5.0", hasNotesForCurrentVersion: true);

        Assert.Equal(ReleaseNotesPromptAction.Show, action);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_UnknownRunningVersion_SkipsAndKeepsTheRecord(string current)
    {
        var action = ReleaseNotesPrompt.Decide("0.5.0", current, hasNotesForCurrentVersion: false);

        Assert.Equal(ReleaseNotesPromptAction.Skip, action);
    }
}
