namespace CLIHub.ViewModels;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Windows;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Updates;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

public class SettingsNavigationTests
{
    [Fact]
    public void Catalog_HasThreeGroupsAndTenPagesInOrder()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.Equal(
            new[] { "General", "Engines & Repos", "System" },
            viewModel.Pages.Select(page => page.Group).Distinct());
        Assert.Equal(10, viewModel.Pages.Count);

        Assert.Equal(
            new[] { "general", "launch-window", "projects-registry", "projects", "agents", "agent-detection", "terminal", "appearance", "telemetry", "updates" },
            viewModel.Pages.Select(page => page.Key));
    }

    [Fact]
    public void Catalog_EveryPageHasAnIcon()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.All(viewModel.Pages, page => Assert.False(string.IsNullOrEmpty(page.Icon)));
    }

    [Fact]
    public void Catalog_ProjectsRegistryPageIsNamedProjects()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.Equal("Projects", viewModel.Pages.Single(page => page.Key == "projects-registry").Title);
    }

    [Fact]
    public void Catalog_ProjectsPageIsNamedProjectScanning()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.Equal("Project Scanning", viewModel.Pages.Single(page => page.Key == "projects").Title);
    }

    [Fact]
    public void Catalog_EveryPageHasDescription()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.All(viewModel.Pages, page => Assert.False(string.IsNullOrWhiteSpace(page.Description)));
    }

    [Fact]
    public void Catalog_PageSlotKeys_AreSetOnlyForThePageSpecificSlots()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.Equal(
            new (string Key, string? HeaderTitleExtraKey, string? HeaderActionKey, string? SummaryRowKey)[]
            {
                ("general", null, null, null),
                ("launch-window", null, null, null),
                ("projects-registry", null, null, "RegistrySummaryCards"),
                ("projects", null, "ProjectsHeaderAction", "ProjectsSummaryCards"),
                ("agents", null, null, "AgentsSummaryCards"),
                ("agent-detection", null, null, "AgentDetectionSummaryCards"),
                ("terminal", null, null, null),
                ("appearance", null, null, null),
                ("telemetry", null, null, null),
                ("updates", "UpdatesHeaderTitleExtra", "UpdatesHeaderAction", null)
            },
            viewModel.Pages.Select(page => (page.Key, page.HeaderTitleExtraKey, page.HeaderActionKey, page.SummaryRowKey)));
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
                CheckForUpdatesOnStartup = true
            });
            this.Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()));
            this.Startup.Setup(x => x.IsEnabled()).Returns(false);
            this.Applier.Setup(x => x.ApplyTheme(It.IsAny<AppTheme>()));
            this.Updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

            var application = new SettingsApplicationService(
                this.Preferences.Object,
                this.Startup.Object,
                this.Applier.Object,
                new Mock<Core.Scanning.IProjectScanner>().Object,
                NullLogger<SettingsApplicationService>.Instance);
            this.ViewModel = new SettingsViewModel(
                application,
                this.Updates.Object,
                this.Updates.Object,
                Mock.Of<IReleaseNotesLauncher>(),
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                new Mock<IProjectDialogService>().Object,
                new Mock<Core.Scanning.IProjectScanner>().Object,
                Mock.Of<IAgentManagementSection>(),
                Mock.Of<IProjectRegistrySection>(),
                new AccentSelectionMemory(),
                Mock.Of<IRuntimeExecutableLocator>(),
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
