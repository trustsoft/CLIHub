namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

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
        public Mock<IUpdateService> Updates { get; } = new();
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
            Applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
            Applier.Setup(x => x.ApplyRuntime(RuntimeKind.WindowsTerminal));
            Applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
            Applier.Setup(x => x.ApplyHotkey(It.IsAny<CLIHub.Core.Hotkeys.HotkeyDefinition>())).Returns(true);
            Applier.Setup(x => x.ApplyStartupUpdateCheck(true));
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
