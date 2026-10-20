namespace CLIHub.ViewModels;

using CLIHub.Core.Agents;
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

    [Fact]
    public void UpdateAvailability_WhenUpdateAvailable_ShowsArrowTextAndFlag()
    {
        var card = CreateCard(isInstalled: true, version: "2.0.32");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33");

        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("2.0.32 → 2.0.33", card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenUpToDate_ShowsNoIndicator()
    {
        var card = CreateCard(isInstalled: true, version: "2.0.33");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpToDate, "2.0.33", "2.0.33");

        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenUnknown_ShowsNoIndicator()
    {
        var card = CreateCard(isInstalled: true, version: "2.0.32");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.Unknown);

        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenSet_RaisesIndicatorNotifications()
    {
        var card = CreateCard(isInstalled: true, version: "2.0.32");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33");

        Assert.Contains(nameof(AgentCardItem.UpdateAvailability), properties);
        Assert.Contains(nameof(AgentCardItem.UpdateAvailabilityText), properties);
        Assert.Contains(nameof(AgentCardItem.HasUpdateAvailable), properties);
    }

    [Fact]
    public void UpdateAvailability_WhenCleared_RaisesIndicatorNotifications()
    {
        var card = CreateCard(isInstalled: true, version: "2.0.32");
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.UpdateAvailability = null;

        Assert.Contains(nameof(AgentCardItem.UpdateAvailabilityText), properties);
        Assert.Contains(nameof(AgentCardItem.HasUpdateAvailable), properties);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.False(card.HasUpdateAvailable);
    }

    private static AgentCardItem CreateCard(bool isInstalled, string version) =>
        new(
            new Plugin { Id = "test-agent", Name = "Test Agent" },
            logoPath: null,
            isInstalled: isInstalled,
            version: version,
            configPath: null);
}
