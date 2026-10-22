namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Configuration;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Windows;
using CLIHub.Core.Models;
using CLIHub.Settings;

public class SettingsApplicationServiceTests
{
    [Fact]
    public void TryCreateDraft_InvalidProbeValue_ReturnsValidationErrorWithoutSideEffects()
    {
        var service = CreateService(
            new Mock<IPreferencesStore>().Object,
            new Mock<IStartupService>().Object,
            new Mock<IPreferenceApplier>().Object);

        var validInput = Input(probeTtlText: "0");

        var success = service.TryCreateDraft(validInput, out var draft, out var error);

        Assert.False(success);
        Assert.Null(draft);
        Assert.Equal("Probe TTL must be a positive number of minutes, or empty for the default.", error);
    }

    [Fact]
    public void Save_ValidDraft_AppliesThenPersistsAndReportsSuccess()
    {
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        preferencesStore.Setup(x => x.Load()).Returns(PreviousPreferences());
        preferencesStore.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()));
        var startup = new Mock<IStartupService>(MockBehavior.Strict);
        startup.Setup(x => x.IsEnabled()).Returns(false);
        var applier = CreateApplier();
        var newHotkey = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x42);
        var draft = new SettingsDraft(
            RuntimeKind.PowerShell,
            PathDisplayStyle.MiddleEllipsis,
            HotkeyParser.Default,
            15,
            30,
            true,
            false,
            false,
            true,
            true,
            false,
            "Information",
            7,
            0,
            PopupScreenPosition.Center,
            AppTheme.Dark,
            "#4CC2FF");
        applier.Setup(x => x.ApplyStartWithWindows(true)).Returns(true);
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis));
        applier.Setup(x => x.ApplyHotkey(HotkeyParser.Default)).Returns(true);
        applier.Setup(x => x.ApplyStartupUpdateCheck(false));
        applier.Setup(x => x.ApplyAccent("#4CC2FF"));
        var service = CreateService(preferencesStore.Object, startup.Object, applier.Object);

        var result = service.Save(draft);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        applier.Verify(x => x.ApplyTheme(draft.AppTheme), Times.Once);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
    }

    [Fact]
    public void Save_HotkeyFailure_RollsBackSystemChangesAndDoesNotPersist()
    {
        var previous = PreviousPreferences();
        previous.AppTheme = AppTheme.Light;
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        preferencesStore.Setup(x => x.Load()).Returns(previous);
        var startup = new Mock<IStartupService>(MockBehavior.Strict);
        startup.Setup(x => x.IsEnabled()).Returns(false);
        var applier = CreateApplier();
        var newHotkey = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x42);
        applier.Setup(x => x.ApplyStartWithWindows(true)).Returns(true);
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis));
        applier.Setup(x => x.ApplyHotkey(It.IsAny<HotkeyDefinition>())).Returns(true);
        applier.Setup(x => x.ApplyHotkey(newHotkey)).Returns(false);
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.GitBash));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
        applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
        applier.Setup(x => x.ApplyAccent("#4CC2FF"));
        var service = CreateService(preferencesStore.Object, startup.Object, applier.Object);

        var result = service.Save(new SettingsDraft(
            RuntimeKind.PowerShell,
            PathDisplayStyle.MiddleEllipsis,
            newHotkey,
            15,
            30,
            true,
            false,
            false,
            true,
            true,
            false,
            "Information",
            7,
            0,
            PopupScreenPosition.Center,
            AppTheme.Dark,
            "#4CC2FF"));

        Assert.False(result.Success);
        Assert.Contains("global hotkey", result.Error, StringComparison.OrdinalIgnoreCase);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
        applier.Verify(x => x.ApplyRuntime(RuntimeKind.GitBash), Times.Once);
        applier.Verify(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim), Times.Once);
        applier.Verify(x => x.ApplyStartWithWindows(false), Times.Once);
        applier.Verify(x => x.ApplyTheme(AppTheme.Dark), Times.Once);
        applier.Verify(x => x.ApplyTheme(previous.AppTheme), Times.Once);
    }

    [Fact]
    public void Save_DefaultAgentId_PersistsValue()
    {
        var captured = new AppPreferences();
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        preferencesStore.Setup(x => x.Load()).Returns(PreviousPreferences());
        preferencesStore
            .Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(update => update(captured));
        var startup = new Mock<IStartupService>(MockBehavior.Strict);
        startup.Setup(x => x.IsEnabled()).Returns(false);
        var applier = CreateApplier();
        applier.Setup(x => x.ApplyStartWithWindows(true)).Returns(true);
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis));
        applier.Setup(x => x.ApplyHotkey(HotkeyParser.Default)).Returns(true);
        applier.Setup(x => x.ApplyStartupUpdateCheck(false));
        applier.Setup(x => x.ApplyAccent("#4CC2FF"));
        var service = CreateService(preferencesStore.Object, startup.Object, applier.Object);

        var result = service.Save(new SettingsDraft(
            RuntimeKind.PowerShell,
            PathDisplayStyle.MiddleEllipsis,
            HotkeyParser.Default,
            15,
            30,
            true,
            false,
            false,
            true,
            true,
            false,
            "Information",
            7,
            0,
            PopupScreenPosition.Center,
            AppTheme.Dark,
            "#4CC2FF",
            DefaultAgentId: "opencode"));

        Assert.True(result.Success);
        Assert.Equal("opencode", captured.DefaultAgentId);
    }

    [Fact]
    public void ApplyScanning_ValidInput_PersistsScanningPreferencesAndReloadsScanner()
    {
        var captured = new AppPreferences
        {
            DefaultRuntime = RuntimeKind.GitBash,
            Hotkey = "Ctrl+Shift+A"
        };
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        preferencesStore
            .Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(update => update(captured));
        var scanner = new Mock<Core.Scanning.IProjectScanner>();
        var service = CreateService(
            preferencesStore.Object,
            new Mock<IStartupService>().Object,
            new Mock<IPreferenceApplier>().Object,
            scanner.Object);

        var result = service.ApplyScanning(new ScanningSettingsInput(
            [@"C:\repos", @"C:\repos", " "],
            3,
            ["node_modules", "node_modules"],
            ProjectDiscoveryMode.Git,
            new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [@"C:\repos"] = new(4, ProjectDiscoveryMode.Agents),
                [@"C:\other"] = new(2, ProjectDiscoveryMode.Git)
            }));

        Assert.True(result.Success);
        Assert.Equal([@"C:\repos"], captured.WatchedDirectories);
        Assert.Equal(3, captured.ScanDepth);
        Assert.Equal(["node_modules"], captured.IgnoredPatterns);
        Assert.Equal(ProjectDiscoveryMode.Git, captured.DiscoveryMode);
        Assert.Equal(4, captured.WatchedDirectorySettings[@"C:\repos"].ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Agents, captured.WatchedDirectorySettings[@"C:\repos"].DiscoveryMode);
        Assert.DoesNotContain(@"C:\other", captured.WatchedDirectorySettings.Keys);
        Assert.Equal(RuntimeKind.GitBash, captured.DefaultRuntime);
        scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void ApplyScanning_OutOfRangeDepth_ReturnsErrorWithoutSideEffects()
    {
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        var scanner = new Mock<Core.Scanning.IProjectScanner>();
        var service = CreateService(
            preferencesStore.Object,
            new Mock<IStartupService>().Object,
            new Mock<IPreferenceApplier>().Object,
            scanner.Object);

        var result = service.ApplyScanning(new ScanningSettingsInput(
            [@"C:\repos"],
            99,
            [],
            ProjectDiscoveryMode.Git,
            null));

        Assert.False(result.Success);
        Assert.Contains("scan depth", result.Error, StringComparison.OrdinalIgnoreCase);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
        scanner.Verify(x => x.Reload(), Times.Never);
    }

    [Fact]
    public void ApplyScanning_OutOfRangeRowDepth_ReturnsErrorWithoutSideEffects()
    {
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        var scanner = new Mock<Core.Scanning.IProjectScanner>();
        var service = CreateService(
            preferencesStore.Object,
            new Mock<IStartupService>().Object,
            new Mock<IPreferenceApplier>().Object,
            scanner.Object);

        var result = service.ApplyScanning(new ScanningSettingsInput(
            [@"C:\repos"],
            2,
            [],
            ProjectDiscoveryMode.Git,
            new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [@"C:\repos"] = new(0, ProjectDiscoveryMode.Git)
            }));

        Assert.False(result.Success);
        Assert.Contains(@"C:\repos", result.Error);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
        scanner.Verify(x => x.Reload(), Times.Never);
    }

    private static SettingsApplicationService CreateService(
        IPreferencesStore preferencesStore,
        IStartupService startupService,
        IPreferenceApplier applier,
        Core.Scanning.IProjectScanner? projectScanner = null) =>
        new(
            preferencesStore,
            startupService,
            applier,
            projectScanner ?? new Mock<Core.Scanning.IProjectScanner>().Object,
            NullLogger<SettingsApplicationService>.Instance);

    private static Mock<IPreferenceApplier> CreateApplier()
    {
        var applier = new Mock<IPreferenceApplier>(MockBehavior.Strict);
        applier.Setup(x => x.ApplyTheme(It.IsAny<AppTheme>()));
        return applier;
    }

    private static SettingsInput Input(string probeTtlText = "", string probeTimeoutText = "") => new(
        RuntimeKind.PowerShell,
        PathDisplayStyle.LeftTrim,
        "Ctrl+Shift+A",
        probeTtlText,
        probeTimeoutText,
        false,
        true,
        true,
        true,
        true,
        false,
        "Information",
        "7",
        "0",
        PopupScreenPosition.Center,
        AppTheme.Dark,
        "#4CC2FF");

    private static AppPreferences PreviousPreferences() => new()
    {
        DefaultRuntime = RuntimeKind.GitBash,
        PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
        Hotkey = "Ctrl+Shift+A",
        CheckForUpdatesOnStartup = true,
        ShowWindowOnStartup = true
    };
}
