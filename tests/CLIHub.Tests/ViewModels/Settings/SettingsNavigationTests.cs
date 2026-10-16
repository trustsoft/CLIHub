namespace CLIHub.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Configuration;
using CLIHub.Core.Windows;
using CLIHub.Core.Models;
using CLIHub.Updates;
using CLIHub.Settings;
using CLIHub.Launching;
using CLIHub.Lifetime;

public class SettingsNavigationTests
{
    [Fact]
    public void Catalog_HasThreeGroupsAndEightPagesInOrder()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.Equal(
            new[] { "General", "Engines & Repos", "System" },
            viewModel.PageGroups.Select(group => group.Title));
        Assert.Equal(8, viewModel.Pages.Count);

        Assert.Equal(
            new[] { "general", "hotkeys", "projects", "agents", "terminal", "appearance", "telemetry", "updates" },
            viewModel.Pages.Select(page => page.Key));
    }

    [Fact]
    public void Catalog_EveryPageHasAnIcon_AndHotkeysTitleMatchesTarget()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.All(viewModel.Pages, page => Assert.False(string.IsNullOrEmpty(page.Icon)));
        Assert.Equal("Hotkeys & Quick Launcher", viewModel.Pages.Single(page => page.Key == "hotkeys").Title);
    }

    [Fact]
    public void Catalog_MarksAllPagesAsImplemented()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.True(viewModel.Pages.Single(page => page.Key == "projects").IsImplemented);
        Assert.All(viewModel.Pages, page => Assert.True(page.IsImplemented));
    }

    [Fact]
    public void DefaultSelectedPage_IsGeneralStartup()
    {
        var fixture = new Fixture();

        fixture.ViewModel.Load();

        Assert.Equal("general", fixture.ViewModel.SelectedPage.Key);
    }

    [Fact]
    public void SelectingPage_RaisesPropertyChanged()
    {
        var viewModel = new Fixture().ViewModel;
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SelectedPage = viewModel.Pages.Single(page => page.Key == "updates");

        Assert.Contains(nameof(SettingsViewModel.SelectedPage), changed);
        Assert.Equal("updates", viewModel.SelectedPage.Key);
    }

    private sealed class Fixture
    {
        public Mock<IPreferencesStore> Preferences { get; } = new(MockBehavior.Strict);
        public Mock<IStartupService> Startup { get; } = new(MockBehavior.Strict);
        public Mock<IPreferenceApplier> Applier { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateWorkflow> Updates { get; } = new();
        public SettingsViewModel ViewModel { get; }

        public Fixture()
        {
            this.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
            {
                DefaultRuntime = RuntimeKind.PowerShell,
                Hotkey = "Ctrl+Shift+A",
                PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
                CheckForUpdatesOnStartup = true,
                ShowWindowOnStartup = true
            });
            this.Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()));
            this.Startup.Setup(x => x.IsEnabled()).Returns(false);
            this.Updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

            var application = new SettingsApplicationService(
                this.Preferences.Object,
                this.Startup.Object,
                this.Applier.Object,
                new Mock<CLIHub.Core.Scanning.IProjectScanner>().Object,
                NullLogger<SettingsApplicationService>.Instance);
            this.ViewModel = new SettingsViewModel(
                application,
                this.Updates.Object,
                this.Updates.Object,
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                new Mock<IProjectDialogService>().Object,
                new Mock<CLIHub.Core.Scanning.IProjectScanner>().Object,
                Mock.Of<IAgentManagementSection>(),
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
