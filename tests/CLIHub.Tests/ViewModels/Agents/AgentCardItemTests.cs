namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   Tests for AgentCardItem state chip formatting and change notifications.
/// </summary>
public sealed class AgentCardItemTests
{
    [Fact]
    public void VersionChipText_WhenInstalledWithVersion_ShowsVersion()
    {
        var card = CreateCard(isInstalled: true, version: "1.2.3");

        Assert.Equal("v1.2.3", card.VersionChipText);
    }

    [Fact]
    public void VersionChipText_WhenNotInstalled_ShowsNotInstalled()
    {
        var card = CreateCard(isInstalled: false, version: "1.2.3");

        Assert.Equal("Not installed", card.VersionChipText);
    }

    [Theory]
    [InlineData("…")]
    [InlineData("unknown")]
    [InlineData("")]
    public void VersionChipText_WhenInstalledWithoutKnownVersion_ShowsInstalled(string version)
    {
        var card = CreateCard(isInstalled: true, version: version);

        Assert.Equal("Installed", card.VersionChipText);
    }

    [Fact]
    public void Version_WhenChanged_RaisesVersionChipTextNotification()
    {
        var card = CreateCard(isInstalled: true, version: "unknown");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.Version = "2.0.0";

        Assert.Contains(nameof(AgentCardItem.Version), properties);
        Assert.Contains(nameof(AgentCardItem.VersionChipText), properties);
        Assert.Equal("v2.0.0", card.VersionChipText);
    }

    [Fact]
    public void IsInstalled_WhenChanged_RaisesVersionChipTextNotification()
    {
        var card = CreateCard(isInstalled: true, version: "1.2.3");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.IsInstalled = false;

        Assert.Contains(nameof(AgentCardItem.VersionChipText), properties);
        Assert.Equal("Not installed", card.VersionChipText);
    }

    private static AgentCardItem CreateCard(bool isInstalled, string version) =>
        new(
            new Plugin { Id = "test-agent", Name = "Test Agent" },
            logoPath: null,
            isInstalled: isInstalled,
            version: version,
            configPath: null);
}
