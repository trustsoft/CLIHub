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

public class SettingsViewModelApplicationTests
{
    [Fact]
    public void Cancel_DoesNotPersistAndRequestsClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;

        fixture.ViewModel.CancelCommand.Execute(null);

        Assert.True(closeRequested);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    [Fact]
    public void Save_ValidDraft_PersistsAndRequestsClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;
        fixture.ViewModel.Load();

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.True(closeRequested);
        Assert.Null(fixture.ViewModel.ValidationError);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
    }

    [Fact]
    public void Save_InvalidDraft_DoesNotPersistOrClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;
        fixture.ViewModel.Load();
        fixture.ViewModel.ProbeTimeoutText = "0";

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.False(closeRequested);
        Assert.Equal("Probe timeout must be a positive number of seconds, or empty for the default.", fixture.ViewModel.ValidationError);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
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
                StartMinimized = true
            });
            this.Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()));
            this.Startup.Setup(x => x.IsEnabled()).Returns(false);
            this.Applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
            this.Applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
            this.Applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
            this.Applier.Setup(x => x.ApplyHotkey(It.IsAny<CLIHub.Core.Hotkeys.HotkeyDefinition>())).Returns(true);
            this.Applier.Setup(x => x.ApplyStartupUpdateCheck(true));
            this.Applier.Setup(x => x.ApplyAccent(It.IsAny<string>()));
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
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                new Mock<IProjectDialogService>().Object,
                new Mock<Core.Scanning.IProjectScanner>().Object,
                Mock.Of<IAgentManagementSection>(),
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
