namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Updates;
using CLIHub.Launching;
using CLIHub.Tests.Builders;
using CLIHub.Tests.Support;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void InstallPluginCommand_WhenPickerCancelled_NeitherInspectsNorInstalls()
    {
        // A strict installer mock fails the test on any call, proving the cancelled picker
        // stops the workflow before inspection.
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        var dialogs = new FakePluginDialogService { PackagePath = null };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        Assert.Equal(1, dialogs.PickerCallCount);
        Assert.Empty(dialogs.ConfirmationPreviews);
        Assert.Empty(dialogs.ValidationErrorsShown);
        Assert.Empty(dialogs.Outcomes);
        installer.VerifyNoOtherCalls();
    }

    [Fact]
    public void InstallPluginCommand_WhenPackageInvalid_ShowsErrorsWithoutConfirmationOrInstall()
    {
        var packagePath = @"C:\packages\demo.clihub-plugin.zip";
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        installer.Setup(x => x.Inspect(packagePath)).Returns(PluginInstallPreview.Invalid(["Missing plugin.json"]));
        // Confirming is pre-allowed so an erroneously shown confirmation would proceed to the
        // unconfigured Install call and fail the strict mock.
        var dialogs = new FakePluginDialogService { PackagePath = packagePath, ConfirmInstallation = true };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        var errors = Assert.Single(dialogs.ValidationErrorsShown);
        Assert.Equal("Missing plugin.json", Assert.Single(errors));
        Assert.Empty(dialogs.ConfirmationPreviews);
        Assert.Empty(dialogs.Outcomes);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void InstallPluginCommand_WhenUserConfirmsReplacement_InstallsWithReplaceExistingTrue()
    {
        var packagePath = @"C:\packages\demo.clihub-plugin.zip";
        var preview = ValidPreview(replacesExisting: true);
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        installer.Setup(x => x.Inspect(packagePath)).Returns(preview);
        installer.Setup(x => x.Install(packagePath, true)).Returns(PluginInstallResult.Succeeded("demo"));
        var dialogs = new FakePluginDialogService { PackagePath = packagePath, ConfirmInstallation = true };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        // The inspected preview is exactly what the confirmation dialog shows, and Install
        // receives the replacement consent the preview reported.
        Assert.Same(preview, Assert.Single(dialogs.ConfirmationPreviews));
        var (outcome, pluginName) = Assert.Single(dialogs.Outcomes);
        Assert.True(outcome.Success);
        Assert.Equal("Demo", pluginName);
        Assert.Empty(dialogs.ValidationErrorsShown);
        installer.VerifyAll();
    }

    [Fact]
    public void InstallPluginCommand_WhenNoExistingPlugin_InstallsWithReplaceExistingFalse()
    {
        var packagePath = @"C:\packages\demo.clihub-plugin.zip";
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        installer.Setup(x => x.Inspect(packagePath)).Returns(ValidPreview(replacesExisting: false));
        installer.Setup(x => x.Install(packagePath, false)).Returns(PluginInstallResult.Succeeded("demo"));
        var dialogs = new FakePluginDialogService { PackagePath = packagePath, ConfirmInstallation = true };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        installer.VerifyAll();
        Assert.Empty(dialogs.ValidationErrorsShown);
    }

    [Fact]
    public void InstallPluginCommand_WhenUserDeclinesConfirmation_DoesNotInstall()
    {
        var packagePath = @"C:\packages\demo.clihub-plugin.zip";
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        installer.Setup(x => x.Inspect(packagePath)).Returns(ValidPreview(replacesExisting: false));
        var dialogs = new FakePluginDialogService { PackagePath = packagePath, ConfirmInstallation = false };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        Assert.Single(dialogs.ConfirmationPreviews);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Empty(dialogs.Outcomes);
        Assert.Empty(dialogs.ValidationErrorsShown);
    }

    [Fact]
    public void InstallPluginCommand_WhenInstallFails_ShowsFailureErrors()
    {
        var packagePath = @"C:\packages\demo.clihub-plugin.zip";
        var installer = new Mock<IPluginInstaller>(MockBehavior.Strict);
        installer.Setup(x => x.Inspect(packagePath)).Returns(ValidPreview(replacesExisting: false));
        installer.Setup(x => x.Install(packagePath, false)).Returns(PluginInstallResult.Failed(["Staging move failed"]));
        var dialogs = new FakePluginDialogService { PackagePath = packagePath, ConfirmInstallation = true };
        var viewModel = new AgentManagementViewModelBuilder().WithPluginInstaller(installer).WithPluginDialogs(dialogs).Build();

        viewModel.InstallPluginCommand.Execute(null);

        var errors = Assert.Single(dialogs.ValidationErrorsShown);
        Assert.Equal("Staging move failed", Assert.Single(errors));
        Assert.Empty(dialogs.Outcomes);
    }

    [Fact]
    public void DisablePluginCommand_OnEnabledCard_MarksDisabledInStateAndReloadsCatalog()
    {
        var state = new InMemoryPluginRegistry();
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var card = viewModel.ViewModel.Cards.Single(c => c.PluginId == "a");

        viewModel.ViewModel.DisablePluginCommand.Execute(card);

        Assert.False(state.Document.Plugins["a"].Enabled);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void EnablePluginCommand_OnDisabledCard_ClearsDisabledInStateAndReloadsCatalog()
    {
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { Enabled = false };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithDisabledPlugins(new DisabledPluginEntry("a", @"C:\plugins\a", PluginOrigin.Installed, "1.0.0"))
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var disabledCard = Assert.Single(viewModel.ViewModel.DisabledCards);

        viewModel.ViewModel.EnablePluginCommand.Execute(disabledCard);

        Assert.True(state.Document.Plugins["a"].Enabled);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void DisablePlugin_SaveFails_SetsMessage()
    {
        var state = new InMemoryPluginRegistry { TrySaveSucceeds = false };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var card = viewModel.ViewModel.Cards.Single(c => c.PluginId == "a");

        viewModel.ViewModel.DisablePluginCommand.Execute(card);

        // The failed write is reported on the card, and the reload still presents the persisted
        // (still enabled) truth instead of a phantom disabled state.
        Assert.Equal("Could not save the plugin state", card.Message);
        Assert.True(card.HasMessage);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void EnablePlugin_SaveFails_SetsMessage()
    {
        var state = new InMemoryPluginRegistry { TrySaveSucceeds = false };
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { Enabled = false };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithDisabledPlugins(new DisabledPluginEntry("a", @"C:\plugins\a", PluginOrigin.Installed, "1.0.0"))
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var disabledCard = Assert.Single(viewModel.ViewModel.DisabledCards);

        viewModel.ViewModel.EnablePluginCommand.Execute(disabledCard);

        // Disabled cards gained Message/HasMessage for exactly this failure path.
        Assert.Equal("Could not save the plugin state", disabledCard.Message);
        Assert.True(disabledCard.HasMessage);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void DisablePlugin_SaveFails_RebuiltCardShowsMessage()
    {
        var state = new InMemoryPluginRegistry { TrySaveSucceeds = false };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .BuildWithMocks();
        // The real catalog raises PluginsChanged from ReloadPlugins, which makes the view model
        // rebuild Cards; without the raise the assertion would land on the invoked card only.
        var catalog = viewModel.Mocks.PluginCatalog;
        catalog.Setup(x => x.ReloadPlugins()).Callback(() => catalog.Raise(x => x.PluginsChanged += null, EventArgs.Empty));
        viewModel.ViewModel.Load(null);
        var card = viewModel.ViewModel.Cards.Single(c => c.PluginId == "a");

        viewModel.ViewModel.DisablePluginCommand.Execute(card);

        var rebuiltCard = viewModel.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.NotSame(card, rebuiltCard);
        Assert.Equal("Could not save the plugin state", rebuiltCard.Message);
        Assert.True(rebuiltCard.HasMessage);
        catalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void EnablePlugin_SaveFails_RebuiltDisabledCardShowsMessage()
    {
        var state = new InMemoryPluginRegistry { TrySaveSucceeds = false };
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { Enabled = false };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithDisabledPlugins(new DisabledPluginEntry("a", @"C:\plugins\a", PluginOrigin.Installed, "1.0.0"))
            .WithPluginState(state)
            .BuildWithMocks();
        var catalog = viewModel.Mocks.PluginCatalog;
        catalog.Setup(x => x.ReloadPlugins()).Callback(() => catalog.Raise(x => x.PluginsChanged += null, EventArgs.Empty));
        viewModel.ViewModel.Load(null);
        var disabledCard = Assert.Single(viewModel.ViewModel.DisabledCards);

        viewModel.ViewModel.EnablePluginCommand.Execute(disabledCard);

        // The reload rebuilds DisabledCards from the persisted (still disabled) state, so the
        // message has to be set on the rebuilt instance to be visible after the command.
        var rebuiltCard = Assert.Single(viewModel.ViewModel.DisabledCards);
        Assert.NotSame(disabledCard, rebuiltCard);
        Assert.Equal("Could not save the plugin state", rebuiltCard.Message);
        Assert.True(rebuiltCard.HasMessage);
        catalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void OpenPluginsFolderCommand_OpensThePluginsDirectoryThroughTheLauncher()
    {
        var launcher = new Mock<IExternalLauncher>();
        var viewModel = new AgentManagementViewModelBuilder().WithLauncher(launcher).Build();

        viewModel.OpenPluginsFolderCommand.Execute(null);

        launcher.Verify(x => x.Open(AppPaths.PluginsDirectory), Times.Once);
    }

    [Fact]
    public void ReloadCatalogCommand_ReloadsTheCatalog()
    {
        var viewModel = new AgentManagementViewModelBuilder().BuildWithMocks();

        viewModel.ViewModel.ReloadCatalogCommand.Execute(null);

        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void ReloadCatalogCommand_SeedsBeforeReloadingCatalog()
    {
        // The in-sequence strict setups fail if either call is skipped or if the reload runs
        // before the seed, proving the manual command mirrors the startup order.
        var sequence = new MockSequence();
        var seeder = new Mock<IPluginSeeder>(MockBehavior.Strict);
        var catalog = new Mock<IPluginCatalog>(MockBehavior.Strict);

        seeder.InSequence(sequence).Setup(x => x.EnsureBuiltinsExist()).Returns(0);
        catalog.InSequence(sequence).Setup(x => x.ReloadPlugins());

        var viewModel = new AgentManagementViewModelBuilder()
            .WithSeeder(seeder)
            .WithPluginCatalog(catalog)
            .Build();

        viewModel.ReloadCatalogCommand.Execute(null);

        seeder.Verify(x => x.EnsureBuiltinsExist(), Times.Once);
        catalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ReloadCatalogCommand_ReloadsRegardlessOfSeedingResult(int seededCount)
    {
        // The seeder returns the number of descriptors written: 0 means nothing needed restoring,
        // a positive count means built-ins were restored. Neither outcome may suppress the rescan.
        // Seeding *failure* needs no view-model case: the seeder swallows its own exceptions
        // (EnsureBuiltinsExist_StateSaveFails_LogsContextAndContinues in PluginSeederTests).
        var seeder = new Mock<IPluginSeeder>();
        seeder.Setup(x => x.EnsureBuiltinsExist()).Returns(seededCount);
        var viewModel = new AgentManagementViewModelBuilder().WithSeeder(seeder).BuildWithMocks();

        viewModel.ViewModel.ReloadCatalogCommand.Execute(null);

        seeder.Verify(x => x.EnsureBuiltinsExist(), Times.Once);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag()
    {
        var viewModel = new AgentManagementViewModelBuilder()
            .WithDisabledPlugins(
                new DisabledPluginEntry("installed-one", @"C:\plugins\installed-one", PluginOrigin.Installed, "1.0.0"),
                new DisabledPluginEntry("unknown-one", @"C:\plugins\unknown-one", null, null))
            .Build();

        viewModel.Load(null);

        Assert.Equal(2, viewModel.DisabledCards.Count);
        Assert.True(viewModel.HasDisabledPlugins);
        var installed = viewModel.DisabledCards.Single(c => c.PluginId == "installed-one");
        Assert.StartsWith("Origin: Installed", installed.IdentityTooltip, StringComparison.Ordinal);
        Assert.Equal("1.0.0", installed.VersionChipText);
        var unknown = viewModel.DisabledCards.Single(c => c.PluginId == "unknown-one");
        Assert.StartsWith("Origin: Unknown", unknown.IdentityTooltip, StringComparison.Ordinal);
        Assert.Equal("Unknown version", unknown.VersionChipText);
    }

    [Fact]
    public void Load_WithoutDisabledPlugins_HidesTheDisabledSection()
    {
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .Build();

        viewModel.Load(null);

        Assert.Empty(viewModel.DisabledCards);
        Assert.False(viewModel.HasDisabledPlugins);
    }

    [Fact]
    public void Load_WithOriginsFromState_PopulatesOriginDataAndCatalogSummary()
    {
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Builtin);
        state.Document.Plugins["b"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.2.0" };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .Build();

        viewModel.Load(null);

        Assert.Equal(PluginOrigin.Builtin, viewModel.Cards.Single(c => c.PluginId == "a").Origin);
        Assert.Equal(PluginOrigin.Installed, viewModel.Cards.Single(c => c.PluginId == "b").Origin);
        Assert.Equal("1.2.0", viewModel.Cards.Single(c => c.PluginId == "b").InstalledVersion);
        Assert.StartsWith("Origin: Built-in", viewModel.Cards.Single(c => c.PluginId == "a").IdentityTooltip, StringComparison.Ordinal);
        Assert.StartsWith("Origin: Installed", viewModel.Cards.Single(c => c.PluginId == "b").IdentityTooltip, StringComparison.Ordinal);
        Assert.StartsWith("Origin: Unknown", viewModel.Cards.Single(c => c.PluginId == "c").IdentityTooltip, StringComparison.Ordinal);
        Assert.Equal("1 built-in · 1 installed · 1 unknown", viewModel.PluginCatalogSummary);
    }

    [Fact]
    public void Load_IdentityTooltip_ListsOriginAndDescriptorMetadataWhenDeclared()
    {
        var plugin = Launchable("a");
        plugin.Version = "1.2.0";
        plugin.Publisher = "Community";
        plugin.Homepage = "https://example.com/a";
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed);
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .Build();

        viewModel.Load(null);

        var tooltip = viewModel.Cards.Single(c => c.PluginId == "a").IdentityTooltip;
        Assert.Contains("Origin: Installed", tooltip, StringComparison.Ordinal);
        Assert.Contains("Version: 1.2.0", tooltip, StringComparison.Ordinal);
        Assert.Contains("Publisher: Community", tooltip, StringComparison.Ordinal);
        Assert.Contains("Homepage: https://example.com/a", tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_IdentityTooltip_ShowsOriginLabelWhenNoMetadataDeclared()
    {
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .Build();

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.Equal("Origin: Unknown", card.IdentityTooltip);
    }

    [Fact]
    public void BrowsePluginsCommand_WhenFeedUrlIsNull_IsDisabled()
    {
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(x => x.Load()).Returns(new AppPreferences { PluginFeedUrl = null });
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPreferencesStore(preferencesStore)
            .Build();

        Assert.True(viewModel.BrowsePluginsCommand.CanExecute(null));
    }

    [Fact]
    public void BrowsePluginsCommand_WhenFeedUrlIsEmptyString_IsDisabled()
    {
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(x => x.Load()).Returns(new AppPreferences { PluginFeedUrl = "" });
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPreferencesStore(preferencesStore)
            .Build();

        Assert.False(viewModel.BrowsePluginsCommand.CanExecute(null));
    }

    [Fact]
    public void BrowsePluginsCommand_WhenFeedUrlIsDefault_IsEnabled()
    {
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(x => x.Load()).Returns(new AppPreferences { PluginFeedUrl = FeedUrlProvider.DefaultFeedUrl });
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPreferencesStore(preferencesStore)
            .Build();

        Assert.True(viewModel.BrowsePluginsCommand.CanExecute(null));
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WhenPublished_ReloadsThePluginCatalog()
    {
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder().WithEventBus(eventBus).BuildWithMocks();

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        built.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WithACompatibleNewerFeedVersion_ShowsThePluginUpdateChip()
    {
        var plugin = Launchable("a", withUpdate: true);
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.0.0" };
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .WithFeedClient(FeedWith(FeedEntry("a", "1.2.0", ">=1.0.0")))
            .WithVersionProvider(CurrentVersion("1.0.0"))
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        var card = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(card.HasPluginUpdate);
        Assert.Equal("1.0.0 → 1.2.0", card.PluginUpdateVersionText);
        Assert.Equal("1.0.0 → 1.2.0", card.UpdateAvailabilityText);
        Assert.True(card.HasUpdateAvailable);

        // The chip is advisory for plugins: the card's Update action runs the agent CLI updater, so
        // a plugin update never enables it.
        Assert.False(card.CanUpdate);
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WhenAnAgentUpdateIsAlsoAvailable_KeepsTheAgentChip()
    {
        var plugin = Launchable("a", withUpdate: true);
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.0.0" };
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .WithFeedClient(FeedWith(FeedEntry("a", "1.2.0", ">=1.0.0")))
            .WithVersionProvider(CurrentVersion("1.0.0"))
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);
        var card = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.9.0", "1.0.0");

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        // Both updates are known, but the agent update takes the chip slot and its versions.
        Assert.True(card.HasPluginUpdate);
        Assert.Equal("1.0.0 → 1.2.0", card.PluginUpdateVersionText);
        Assert.Equal("0.9.0 → 1.0.0", card.UpdateAvailabilityText);
        Assert.True(card.HasUpdateAvailable);
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WhenTheFeedEntryIsIncompatible_ShowsNoPluginChip()
    {
        var plugin = Launchable("a");
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.0.0" };
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .WithFeedClient(FeedWith(FeedEntry("a", "1.2.0", ">=2.0.0")))
            .WithVersionProvider(CurrentVersion("1.0.0"))
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        var card = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.HasPluginUpdate);
        Assert.Null(card.PluginUpdateVersionText);
        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WhenTheFeedIsNotInstalledFromTheRegistry_ShowsNoPluginChip()
    {
        var plugin = Launchable("a");
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithFeedClient(FeedWith(FeedEntry("a", "1.2.0", ">=1.0.0")))
            .WithVersionProvider(CurrentVersion("1.0.0"))
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        // No registry entry means the plugin was never installed from a package or the feed, so
        // there is no recorded version to update from (Browse lists it as merely available).
        var card = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.HasPluginUpdate);
    }

    [Fact]
    public void PluginFeedUpdatedEvent_WhenTheFeedCannotBeLoaded_LeavesTheCardsUnchanged()
    {
        var plugin = Launchable("a");
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.0.0" };
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        var card = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.HasPluginUpdate);
        built.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesFromPluginFeedUpdated()
    {
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder().WithEventBus(eventBus).BuildWithMocks();
        built.ViewModel.Dispose();

        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));

        built.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Never);
    }

    [Fact]
    public void Load_AfterAFeedUpdate_ReappliesThePluginUpdateChip()
    {
        var plugin = Launchable("a");
        var state = new InMemoryPluginRegistry();
        state.Document.Plugins["a"] = new PluginRegistryEntry(PluginOrigin.Installed) { InstalledVersion = "1.0.0" };
        var eventBus = CreateEventBus();
        var built = new AgentManagementViewModelBuilder()
            .WithPlugins([plugin])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .WithFeedClient(FeedWith(FeedEntry("a", "1.2.0", ">=1.0.0")))
            .WithVersionProvider(CurrentVersion("1.0.0"))
            .WithEventBus(eventBus)
            .BuildWithMocks();
        built.ViewModel.Load(null);
        eventBus.Publish(new PluginFeedUpdatedEvent("feed-hash"));
        var firstCard = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(firstCard.HasPluginUpdate);

        // A catalog reload rebuilds the cards; the known feed entries are re-applied to the new
        // instances so the indicator survives the rebuild.
        built.ViewModel.Load(null);

        var secondCard = built.ViewModel.Cards.Single(c => c.PluginId == "a");
        Assert.NotSame(firstCard, secondCard);
        Assert.True(secondCard.HasPluginUpdate);
        Assert.Equal("1.0.0 → 1.2.0", secondCard.PluginUpdateVersionText);
    }

    private static DomainEventBus CreateEventBus() => new(NullLogger<DomainEventBus>.Instance);

    private static Mock<IPluginFeedClient> FeedWith(params PluginFeedEntry[] entries)
    {
        var feedClient = new Mock<IPluginFeedClient>();
        feedClient
            .Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, new PluginFeedIndex(2, null, entries), []));
        return feedClient;
    }

    private static Mock<IUpdateVersionProvider> CurrentVersion(string version)
    {
        var versionProvider = new Mock<IUpdateVersionProvider>();
        versionProvider.Setup(x => x.GetCurrentVersion()).Returns(version);
        return versionProvider;
    }

    private static PluginFeedEntry FeedEntry(string id, string version, string compatibility) =>
        new(
            id,
            id,
            "Description",
            version,
            1,
            null,
            "Publisher",
            "https://example.com",
            null,
            null,
            null,
            null,
            compatibility,
            "stable",
            "{}",
            null);

    /// <summary>
    ///   Creates a valid inspection preview for the demo package used by the install
    ///   workflow tests.
    /// </summary>
    /// <param name="replacesExisting"> Whether the preview reports an existing installation. </param>
    /// <returns> The valid preview. </returns>
    private static PluginInstallPreview ValidPreview(bool replacesExisting) =>
        new(true,
            "demo",
            "Demo",
            "1.0.0",
            "Community",
            "https://example.com/demo",
            [new CommandPreview("launch", "demo", "--run")],
            replacesExisting,
            replacesExisting ? "0.9.0" : null,
            []);
}
