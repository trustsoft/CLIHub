namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Launching;
using CLIHub.Tests.Builders;
using CLIHub.Tests.Support;

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
        var state = new InMemoryPluginStateRepository();
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a")])
            .WithDetection(Detection())
            .WithVersions(Versions())
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var card = viewModel.ViewModel.Cards.Single(c => c.PluginId == "a");

        viewModel.ViewModel.DisablePluginCommand.Execute(card);

        Assert.True(state.Document.Plugins["a"].Disabled);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void EnablePluginCommand_OnDisabledCard_ClearsDisabledInStateAndReloadsCatalog()
    {
        var state = new InMemoryPluginStateRepository();
        state.Document.Plugins["a"] = new PluginStateEntry(PluginOrigin.Installed) { Disabled = true };
        var viewModel = new AgentManagementViewModelBuilder()
            .WithDisabledPlugins(new DisabledPluginEntry("a", @"C:\plugins\a", PluginOrigin.Installed, "1.0.0"))
            .WithPluginState(state)
            .BuildWithMocks();
        viewModel.ViewModel.Load(null);
        var disabledCard = Assert.Single(viewModel.ViewModel.DisabledCards);

        viewModel.ViewModel.EnablePluginCommand.Execute(disabledCard);

        Assert.False(state.Document.Plugins["a"].Disabled);
        Assert.Equal(1, state.SaveCount);
        viewModel.Mocks.PluginCatalog.Verify(x => x.ReloadPlugins(), Times.Once);
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
        var state = new InMemoryPluginStateRepository();
        state.Document.Plugins["a"] = new PluginStateEntry(PluginOrigin.Builtin);
        state.Document.Plugins["b"] = new PluginStateEntry(PluginOrigin.Installed) { InstalledVersion = "1.2.0" };
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
        var state = new InMemoryPluginStateRepository();
        state.Document.Plugins["a"] = new PluginStateEntry(PluginOrigin.Installed);
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
