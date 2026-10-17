namespace CLIHub.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
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
    public void ShowReleaseNotesCommand_Execute_OpensReleaseNotes()
    {
        var fixture = new Fixture();

        fixture.ViewModel.ShowReleaseNotesCommand.Execute(null);

        fixture.ReleaseNotes.Verify(x => x.ShowReleaseNotes(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpToDate_SetsCurrentStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("CLIHub is up to date", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Up to date", fixture.ViewModel.UpdateStatusBadge);
        Assert.Equal(SettingBadgeTone.Success, fixture.ViewModel.UpdateStatusBadgeTone);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailable_IncludesAvailableVersion()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpdateAvailable, "1.0.0", "1.1.0"));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update available", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Update available", fixture.ViewModel.UpdateStatusBadge);
        Assert.Equal(SettingBadgeTone.Accent, fixture.ViewModel.UpdateStatusBadgeTone);
        Assert.Contains("1.1.0", fixture.ViewModel.UpdateStatusDetail);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_NotInstalled_SetsUnavailableStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.NotInstalled, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Updates unavailable", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Not installed", fixture.ViewModel.UpdateStatusBadge);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Failed_SetsFailureStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.Failed, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update check failed", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Failed", fixture.ViewModel.UpdateStatusBadge);
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
        public Mock<IReleaseNotesLauncher> ReleaseNotes { get; } = new();
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
                this.ReleaseNotes.Object,
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                new Mock<IProjectDialogService>().Object,
                new Mock<Core.Scanning.IProjectScanner>().Object,
                Mock.Of<IAgentManagementSection>(),
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
