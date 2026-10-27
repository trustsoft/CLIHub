namespace CLIHub.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

/// <summary>
///   Tests for AgentCardItem install-state chip formatting and change notifications.
/// </summary>
public sealed class AgentCardItemTests
{
    [Fact]
    public void VersionChipText_WhenInstalledWithVersion_ShowsVersion()
    {
        var card = CreateCard(AgentInstallState.Installed, "1.2.3");

        Assert.Equal("v1.2.3", card.VersionChipText);
    }

    [Fact]
    public void VersionChipText_WhenInstalledWithoutVersion_ShowsInstalled()
    {
        var card = CreateCard(AgentInstallState.Installed, null);

        Assert.Equal("Installed", card.VersionChipText);
    }

    [Fact]
    public void VersionChipText_WhenNotInstalled_ShowsNotInstalled()
    {
        // The version is irrelevant in this state; the chip never derives text from it.
        var card = CreateCard(AgentInstallState.NotInstalled, "1.2.3");

        Assert.Equal("Not installed", card.VersionChipText);
    }

    [Fact]
    public void VersionChipText_WhenDetecting_ShowsDetecting()
    {
        var card = CreateCard(AgentInstallState.Detecting, null);

        Assert.Equal("Detecting…", card.VersionChipText);
    }

    [Fact]
    public void Version_WhenChanged_RaisesVersionChipTextNotification()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.Version = "2.0.0";

        Assert.Contains(nameof(AgentCardItem.Version), properties);
        Assert.Contains(nameof(AgentCardItem.VersionChipText), properties);
        Assert.Equal("v2.0.0", card.VersionChipText);
    }

    [Fact]
    public void InstallState_WhenChanged_RaisesDerivedPropertyNotifications()
    {
        var card = CreateCard(AgentInstallState.Installed, "1.2.3");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.InstallState = AgentInstallState.NotInstalled;

        Assert.Contains(nameof(AgentCardItem.InstallState), properties);
        Assert.Contains(nameof(AgentCardItem.IsInstalled), properties);
        Assert.Contains(nameof(AgentCardItem.VersionChipText), properties);
        Assert.Equal("Not installed", card.VersionChipText);
    }

    [Theory]
    [InlineData(AgentInstallState.Installed, true)]
    [InlineData(AgentInstallState.Detecting, false)]
    [InlineData(AgentInstallState.NotInstalled, false)]
    public void IsInstalled_WhenDerivedFromInstallState_IsTrueOnlyWhenInstalled(AgentInstallState state, bool expected)
    {
        var card = CreateCard(state, null);

        Assert.Equal(expected, card.IsInstalled);
    }

    [Theory]
    [InlineData(AgentInstallState.Installed, "2.0.32")]
    [InlineData(AgentInstallState.Installed, null)]
    public void DisplayName_WhenInstalled_ShowsNameWithVersionOnlyWhenResolved(AgentInstallState state, string? version)
    {
        var option = new AgentOption("agent", "OpenClaude", version, state);

        Assert.Equal(version is null ? "OpenClaude" : "OpenClaude (2.0.32)", option.DisplayName);
    }

    [Fact]
    public void DisplayName_WhenNotInstalled_ShowsCapitalizedNotInstalledSuffix()
    {
        // Deliberately capitalized, unlike the runtime selector's lowercase suffix.
        var option = new AgentOption("agent", "OpenClaude", null, AgentInstallState.NotInstalled);

        Assert.Equal("OpenClaude (Not installed)", option.DisplayName);
    }

    [Fact]
    public void DisplayName_WhenDetecting_ShowsNameOnly()
    {
        var option = new AgentOption("agent", "OpenClaude", null, AgentInstallState.Detecting);

        Assert.Equal("OpenClaude", option.DisplayName);
        Assert.False(option.IsInstalled);
    }

    [Fact]
    public void DisplayName_WhenNotInstalledWithResolvedVersion_NeverRendersTheVersion()
    {
        var option = new AgentOption("agent", "OpenClaude", "1.2.3", AgentInstallState.NotInstalled);

        Assert.Equal("OpenClaude (Not installed)", option.DisplayName);
    }

    [Theory]
    [InlineData(AgentInstallState.Installed, true)]
    [InlineData(AgentInstallState.Detecting, false)]
    [InlineData(AgentInstallState.NotInstalled, false)]
    public void OptionIsInstalled_WhenDerivedFromInstallState_DisablesOnlyNotInstalled(AgentInstallState state, bool expected)
    {
        var option = new AgentOption("agent", "OpenClaude", null, state);

        Assert.Equal(expected, option.IsInstalled);
    }

    [Fact]
    public void UpdateAvailability_WhenUpdateAvailable_ShowsArrowTextAndFlag()
    {
        var card = CreateCard(AgentInstallState.Installed, "2.0.32");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33");

        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("2.0.32 → 2.0.33", card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenUpToDate_ShowsNoIndicator()
    {
        var card = CreateCard(AgentInstallState.Installed, "2.0.33");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpToDate, "2.0.33", "2.0.33");

        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenUnknown_ShowsNoIndicator()
    {
        var card = CreateCard(AgentInstallState.Installed, "2.0.32");

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.Unknown);

        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
    }

    [Fact]
    public void UpdateAvailability_WhenNotInstalledWithDeclaredUpdateCheck_ShowsNoIndicator()
    {
        // The card never reads Plugin.UpdateCheck itself; the declared block is the scenario's
        // precondition — the real availability pass only ever runs for agents that declare it.
        var plugin = new Plugin
        {
            Id = "test-agent",
            Name = "Test Agent",
            UpdateCheck = new PluginUpdateCheck("npm", Package: "test-pkg")
        };
        var card = new AgentCardItem(
            plugin,
            logoPath: null,
            isInstalled: false,
            version: null,
            configPath: null);

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.Unknown);

        Assert.Null(card.UpdateAvailabilityText);
        Assert.False(card.HasUpdateAvailable);
    }

    [Fact]
    public void UpdateAvailability_WhenSet_RaisesIndicatorNotifications()
    {
        var card = CreateCard(AgentInstallState.Installed, "2.0.32");
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
        var card = CreateCard(AgentInstallState.Installed, "2.0.32");
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33");
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.UpdateAvailability = null;

        Assert.Contains(nameof(AgentCardItem.UpdateAvailabilityText), properties);
        Assert.Contains(nameof(AgentCardItem.HasUpdateAvailable), properties);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.False(card.HasUpdateAvailable);
    }

    [Theory]
    [InlineData(AgentInstallState.Installed, false, false, true)]
    [InlineData(AgentInstallState.Installed, false, true, false)]
    [InlineData(AgentInstallState.Installed, true, false, false)]
    [InlineData(AgentInstallState.Installed, true, true, false)]
    [InlineData(AgentInstallState.Detecting, false, false, false)]
    [InlineData(AgentInstallState.Detecting, false, true, false)]
    [InlineData(AgentInstallState.Detecting, true, false, false)]
    [InlineData(AgentInstallState.Detecting, true, true, false)]
    [InlineData(AgentInstallState.NotInstalled, false, false, false)]
    [InlineData(AgentInstallState.NotInstalled, false, true, false)]
    [InlineData(AgentInstallState.NotInstalled, true, false, false)]
    [InlineData(AgentInstallState.NotInstalled, true, true, false)]
    public void CanCheckUpdates_WhenDerivedFromInstallAndBusyState_IsTrueOnlyForInstalledIdleCard(
        AgentInstallState state,
        bool isDetecting,
        bool isCheckingUpdates,
        bool expected)
    {
        var card = CreateCard(state, null);
        card.IsDetecting = isDetecting;
        card.IsCheckingUpdates = isCheckingUpdates;

        Assert.Equal(expected, card.CanCheckUpdates);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void CanDetect_WhenDerivedFromBusyState_IsTrueOnlyForIdleCard(
        bool isDetecting,
        bool isCheckingUpdates,
        bool expected)
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        card.IsDetecting = isDetecting;
        card.IsCheckingUpdates = isCheckingUpdates;

        Assert.Equal(expected, card.CanDetect);
    }

    [Fact]
    public void IsCheckingUpdates_WhenSet_RaisesCanCheckUpdatesNotification()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.IsCheckingUpdates = true;

        Assert.Contains(nameof(AgentCardItem.IsCheckingUpdates), properties);
        Assert.Contains(nameof(AgentCardItem.CanCheckUpdates), properties);
        Assert.False(card.CanCheckUpdates);
    }

    [Fact]
    public void IsCheckingUpdates_WhenSet_AlsoRaisesCanDetectNotification()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.IsCheckingUpdates = true;

        Assert.Contains(nameof(AgentCardItem.CanDetect), properties);
        Assert.False(card.CanDetect);
    }

    [Fact]
    public void IsDetecting_WhenSet_RaisesCanDetectAndCanCheckUpdatesNotifications()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.IsDetecting = true;

        Assert.Contains(nameof(AgentCardItem.IsDetecting), properties);
        Assert.Contains(nameof(AgentCardItem.CanDetect), properties);
        Assert.Contains(nameof(AgentCardItem.CanCheckUpdates), properties);
        Assert.False(card.CanDetect);
        Assert.False(card.CanCheckUpdates);
    }

    [Fact]
    public void InstallState_WhenChangedToNotInstalled_RaisesCanCheckUpdatesNotification()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.InstallState = AgentInstallState.NotInstalled;

        Assert.Contains(nameof(AgentCardItem.CanCheckUpdates), properties);
        Assert.False(card.CanCheckUpdates);
    }

    [Fact]
    public void InstallState_WhenChangedToNotInstalled_RaisesCanDetectNotification()
    {
        var card = CreateCard(AgentInstallState.Installed, null);
        var properties = new List<string?>();
        card.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        card.InstallState = AgentInstallState.NotInstalled;

        Assert.Contains(nameof(AgentCardItem.CanDetect), properties);
    }

    private static AgentCardItem CreateCard(AgentInstallState state, string? version) =>
        new(
            new Plugin { Id = "test-agent", Name = "Test Agent" },
            logoPath: null,
            isInstalled: state == AgentInstallState.Installed,
            version: version,
            configPath: null)
        {
            InstallState = state
        };
}
