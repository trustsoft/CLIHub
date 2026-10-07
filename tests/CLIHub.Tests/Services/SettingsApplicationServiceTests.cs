namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

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
            false);
        applier.Setup(x => x.ApplyStartWithWindows(true)).Returns(true);
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis));
        applier.Setup(x => x.ApplyHotkey(HotkeyParser.Default)).Returns(true);
        applier.Setup(x => x.ApplyStartupUpdateCheck(false));
        var service = CreateService(preferencesStore.Object, startup.Object, applier.Object);

        var result = service.Save(draft);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
    }

    [Fact]
    public void Save_HotkeyFailure_RollsBackSystemChangesAndDoesNotPersist()
    {
        var previous = PreviousPreferences();
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
        applier.Setup(x => x.ApplyRuntime(RuntimeKind.WindowsTerminal));
        applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
        applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
        var service = CreateService(preferencesStore.Object, startup.Object, applier.Object);

        var result = service.Save(new SettingsDraft(
            RuntimeKind.PowerShell,
            PathDisplayStyle.MiddleEllipsis,
            newHotkey,
            15,
            30,
            true,
            false,
            false));

        Assert.False(result.Success);
        Assert.Contains("global hotkey", result.Error, StringComparison.OrdinalIgnoreCase);
        preferencesStore.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
        applier.Verify(x => x.ApplyRuntime(RuntimeKind.WindowsTerminal), Times.Once);
        applier.Verify(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim), Times.Once);
        applier.Verify(x => x.ApplyStartWithWindows(false), Times.Once);
    }

    private static SettingsApplicationService CreateService(
        IPreferencesStore preferencesStore,
        IStartupService startupService,
        IPreferenceApplier applier) =>
        new(
            preferencesStore,
            startupService,
            applier,
            NullLogger<SettingsApplicationService>.Instance);

    private static Mock<IPreferenceApplier> CreateApplier() => new(MockBehavior.Strict);

    private static SettingsInput Input(string probeTtlText = "", string probeTimeoutText = "") => new(
        RuntimeKind.WindowsTerminal,
        PathDisplayStyle.LeftTrim,
        "Ctrl+Shift+A",
        probeTtlText,
        probeTimeoutText,
        false,
        true,
        true);

    private static AppPreferences PreviousPreferences() => new()
    {
        DefaultRuntime = RuntimeKinds.WindowsTerminalToken,
        PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
        Hotkey = "Ctrl+Shift+A",
        CheckForUpdatesOnStartup = true,
        ShowWindowOnStartup = true
    };
}
