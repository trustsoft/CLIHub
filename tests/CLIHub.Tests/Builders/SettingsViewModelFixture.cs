namespace CLIHub.Tests.Builders;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Scanning;
using CLIHub.Core.Windows;
using CLIHub.Controls;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Updates;
using CLIHub.ViewModels;

internal sealed class SettingsViewModelFixture
{
    public Mock<IPreferencesStore> Preferences { get; } = new(MockBehavior.Strict);
    public Mock<IStartupService> Startup { get; } = new(MockBehavior.Strict);
    public Mock<IPreferenceApplier> Applier { get; } = new(MockBehavior.Strict);
    public Mock<IUpdateWorkflow> Updates { get; } = new();
    public Mock<IReleaseNotesLauncher> ReleaseNotes { get; } = new();
    public Mock<IRuntimeExecutableLocator> RuntimeExecutables { get; } = new();
    public Mock<IProjectScanner> Scanner { get; } = new();
    public Mock<IProjectDialogService> Dialogs { get; } = new();
    public Mock<IAgentManagementSection> Agents { get; }
    public AccentSelectionMemory AccentMemory { get; }
    public AppPreferences CapturedPreferences { get; } = new();
    public SettingsViewModel ViewModel { get; }

    public SettingsViewModelFixture(string accentColor = AccentPalette.DefaultHex,
                                    AccentSelectionMemory? accentMemory = null,
                                    Mock<IAgentManagementSection>? agentsState = null)
    {
        this.Agents = agentsState ?? new Mock<IAgentManagementSection>();
        this.AccentMemory = accentMemory ?? new AccentSelectionMemory();
        this.Preferences.Setup(x => x.Load()).Returns(CreatePreferences(accentColor: accentColor));
        this.Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(update => update(this.CapturedPreferences));
        this.Startup.Setup(x => x.IsEnabled()).Returns(false);
        this.Applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
        this.Applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
        this.Applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
        this.Applier.Setup(x => x.ApplyHotkey(It.IsAny<CLIHub.Core.Hotkeys.HotkeyDefinition>())).Returns(true);
        this.Applier.Setup(x => x.ApplyAccent(It.IsAny<string>()));
        this.Applier.Setup(x => x.ApplyTheme(It.IsAny<AppTheme>()));
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
            this.Agents.Object,
            Mock.Of<IProjectRegistrySection>(),
            this.AccentMemory,
            this.RuntimeExecutables.Object,
            NullLogger<SettingsViewModel>.Instance);
    }

    internal static AppPreferences CreatePreferences(string accentColor = AccentPalette.DefaultHex,
                                                     RuntimeKind defaultRuntime = RuntimeKind.PowerShell,
                                                     string hotkey = "Ctrl+Shift+A",
                                                     string pathDisplayStyle = PathDisplayStyles.LeftTrimToken,
                                                     bool checkForUpdatesOnStartup = true,
                                                     bool startMinimized = true) => new AppPreferences
    {
        DefaultRuntime = defaultRuntime,
        Hotkey = hotkey,
        PathDisplayStyle = pathDisplayStyle,
        CheckForUpdatesOnStartup = checkForUpdatesOnStartup,
        StartMinimized = startMinimized,
        AccentColor = accentColor
    };
}
