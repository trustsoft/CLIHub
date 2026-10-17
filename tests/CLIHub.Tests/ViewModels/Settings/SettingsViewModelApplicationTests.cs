namespace CLIHub.ViewModels;

using System.IO;

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
    public void Load_DefaultAccent_SelectsSkyPreset()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        Assert.Equal("Sky", fixture.ViewModel.SelectedAccent.Name);
        Assert.False(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void Load_CustomStoredAccent_SelectsCustomEntry()
    {
        var fixture = new Fixture("#123456");
        fixture.ViewModel.Load();

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("#123456", fixture.ViewModel.AccentText);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_Preset_UpdatesTextAndLabel()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");

        Assert.Equal("#10B981", fixture.ViewModel.AccentText);
        Assert.Equal("Active: Emerald", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_CustomValue_SelectsCustomEntry()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.AccentText = "#123456";

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_PartialValue_SelectsCustomEntry()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.AccentText = "#12";

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_KeepsCurrentColor()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#4CC2FF", fixture.ViewModel.AccentText);
        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomAfterPreset_RestoresLastCustomValue()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.AccentText = "#123456";

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.AccentText);
        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_RestoresCustomAcrossSettingsWindows()
    {
        var memory = new AccentSelectionMemory();
        var first = new Fixture(accentMemory: memory);
        first.ViewModel.Load();
        first.ViewModel.AccentText = "#123456";
        first.ViewModel.SelectedAccent = first.ViewModel.AccentOptions.First(choice => choice.Name == "Teal");

        var second = new Fixture(accentMemory: memory);
        second.ViewModel.Load();
        second.ViewModel.SelectedAccent = second.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", second.ViewModel.AccentText);
    }

    [Fact]
    public void Load_CustomStoredAccent_RestoresWhenCustomReselected()
    {
        var fixture = new Fixture("#123456");
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.AccentText);
    }

    [Fact]
    public void AddWatchedDirectory_ExistingAbsolutePath_AddsRootAndClearsInput()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.NewWatchedDirectoryText = path;

        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        Assert.Contains(path, fixture.ViewModel.WatchedDirectories);
        Assert.Equal(string.Empty, fixture.ViewModel.NewWatchedDirectoryText);
        Assert.False(fixture.ViewModel.HasWatchedDirectoryError);
        Assert.Equal("1 root monitored", fixture.ViewModel.WatchedRootsSummary);
    }

    [Fact]
    public void AddWatchedDirectory_MissingFolder_ReportsError()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
        fixture.ViewModel.NewWatchedDirectoryText = path;

        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        Assert.Empty(fixture.ViewModel.WatchedDirectories);
        Assert.True(fixture.ViewModel.HasWatchedDirectoryError);
    }

    [Fact]
    public void RemoveWatchedDirectoryCommand_RemovesRoot()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.NewWatchedDirectoryText = path;
        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        fixture.ViewModel.RemoveWatchedDirectoryCommand.Execute(path);

        Assert.Empty(fixture.ViewModel.WatchedDirectories);
        Assert.Equal("No roots monitored", fixture.ViewModel.WatchedRootsSummary);
    }

    [Fact]
    public void RemoveIgnoredPatternCommand_RemovesPattern()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.NewIgnoredPatternText = "custom-ignored-dir";
        fixture.ViewModel.AddIgnoredPatternCommand.Execute(null);

        fixture.ViewModel.RemoveIgnoredPatternCommand.Execute("custom-ignored-dir");

        Assert.DoesNotContain("custom-ignored-dir", fixture.ViewModel.IgnoredPatterns);
    }

    [Fact]
    public void ScanSummary_ReflectsDepthAndDiscoveryMode()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.ScanDepthText = "3";

        Assert.Equal("Both · recursive depth 3", fixture.ViewModel.ScanSummary);
    }

    [Fact]
    public void SelectedScanDepth_SetOption_UpdatesTextAndSummary()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var option = fixture.ViewModel.ScanDepthOptions.First(item => item.Value == 4);

        fixture.ViewModel.SelectedScanDepth = option;

        Assert.Equal("4", fixture.ViewModel.ScanDepthText);
        Assert.Equal("Both · recursive depth 4", fixture.ViewModel.ScanSummary);
    }

    [Fact]
    public void ScanDepthOptions_UsePlainLevelLabels()
    {
        var fixture = new Fixture();

        Assert.Equal(
            new[]
            {
                "1 level",
                "2 levels (default)",
                "3 levels",
                "4 levels",
                "5 levels",
                "6 levels",
                "7 levels",
                "8 levels"
            },
            fixture.ViewModel.ScanDepthOptions.Select(option => option.Name));
    }

    [Fact]
    public void Load_ScannerStatus_PopulatesSummary()
    {
        var fixture = new Fixture();
        fixture.Scanner.SetupGet(scanner => scanner.Status).Returns(
            new Core.Scanning.ProjectScanStatus(
                Core.Scanning.ProjectScanState.Idle,
                true,
                false,
                DateTimeOffset.UtcNow,
                7,
                null));

        fixture.ViewModel.Load();

        Assert.Equal(7, fixture.ViewModel.DiscoveredProjectCount);
        Assert.Equal("Watching", fixture.ViewModel.WatcherStateLabel);
        Assert.StartsWith("Last scan", fixture.ViewModel.WatcherDetailText);
    }

    [Fact]
    public void ScanNowCommand_Execute_ReloadsScanner()
    {
        var fixture = new Fixture();

        fixture.ViewModel.ScanNowCommand.Execute(null);

        fixture.Scanner.Verify(scanner => scanner.Reload(), Times.Once);
    }

    [Fact]
    public void AddWatchedDirectoryFromPicker_SelectedFolder_AddsRoot()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.Dialogs.Setup(dialogs => dialogs.SelectFolder(It.IsAny<string>())).Returns(path);

        fixture.ViewModel.AddWatchedDirectoryFromPickerCommand.Execute(null);

        Assert.Contains(path, fixture.ViewModel.WatchedDirectories);
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
        public Mock<Core.Scanning.IProjectScanner> Scanner { get; } = new();
        public Mock<IProjectDialogService> Dialogs { get; } = new();
        public AccentSelectionMemory AccentMemory { get; }
        public SettingsViewModel ViewModel { get; }

        public Fixture(string accentColor = AccentPalette.DefaultHex, AccentSelectionMemory? accentMemory = null)
        {
            this.AccentMemory = accentMemory ?? new AccentSelectionMemory();
            this.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
            {
                DefaultRuntime = RuntimeKind.PowerShell,
                Hotkey = "Ctrl+Shift+A",
                PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
                CheckForUpdatesOnStartup = true,
                StartMinimized = true,
                AccentColor = accentColor
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
                this.Scanner.Object,
                NullLogger<SettingsApplicationService>.Instance);
            this.ViewModel = new SettingsViewModel(
                application,
                this.Updates.Object,
                this.Updates.Object,
                this.ReleaseNotes.Object,
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                this.Dialogs.Object,
                this.Scanner.Object,
                Mock.Of<IAgentManagementSection>(),
                this.AccentMemory,
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
