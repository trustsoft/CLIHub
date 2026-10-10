namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

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
    public void Catalog_MarksPagesWithoutSettingsAsUnimplemented()
    {
        var viewModel = new Fixture().ViewModel;

        Assert.False(viewModel.Pages.Single(page => page.Key == "projects").IsImplemented);
        Assert.False(viewModel.Pages.Single(page => page.Key == "telemetry").IsImplemented);
        Assert.True(viewModel.Pages.Single(page => page.Key == "general").IsImplemented);
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
            Preferences.Setup(x => x.Load()).Returns(new AppPreferences
            {
                DefaultRuntime = RuntimeKind.WindowsTerminal,
                Hotkey = "Ctrl+Shift+A",
                PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
                CheckForUpdatesOnStartup = true,
                ShowWindowOnStartup = true
            });
            Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()));
            Startup.Setup(x => x.IsEnabled()).Returns(false);
            Updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

            var application = new SettingsApplicationService(
                Preferences.Object,
                Startup.Object,
                Applier.Object,
                NullLogger<SettingsApplicationService>.Instance);
            ViewModel = new SettingsViewModel(
                application,
                Updates.Object,
                Updates.Object,
                new Mock<IApplicationOperationLifetime>().Object,
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
