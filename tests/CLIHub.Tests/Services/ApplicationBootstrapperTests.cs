namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

public class ApplicationBootstrapperTests
{
    [Fact]
    public void Start_FirstInstance_ExecutesStartupInOrder()
    {
        var sequence = new MockSequence();
        var fixture = new BootstrapperFixture();
        var preferences = new AppPreferences
        {
            ShowWindowOnStartup = true,
            CheckForUpdatesOnStartup = true
        };

        fixture.Guard.InSequence(sequence).SetupGet(x => x.IsFirstInstance).Returns(true);
        fixture.Plugin.InSequence(sequence).Setup(x => x.Initialize());
        fixture.Preferences.InSequence(sequence).Setup(x => x.Load()).Returns(preferences);
        fixture.StartupPreferences.InSequence(sequence).Setup(x => x.Apply(preferences));
        fixture.UiFactory.InSequence(sequence).Setup(x => x()).Returns(fixture.Ui.Object);
        fixture.DownloadFactory.InSequence(sequence).Setup(x => x()).Returns(fixture.Download.Object);
        fixture.RequestSourceFactory.InSequence(sequence).Setup(x => x()).Returns(fixture.RequestSource.Object);
        fixture.HotkeyFactory.InSequence(sequence).Setup(x => x()).Returns(fixture.Hotkey.Object);
        fixture.Ui.InSequence(sequence).SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object);
        fixture.Ui.Setup(x => x.ShowLaunchWindow());
        fixture.Hotkey.InSequence(sequence).Setup(x => x.Register(preferences));
        fixture.ReleaseNotes.InSequence(sequence).Setup(x => x.Evaluate(preferences));
        fixture.UpdateStartup.InSequence(sequence)
            .Setup(x => x.CheckAsync(true, It.IsAny<Action<string>>()))
            .Returns(Task.CompletedTask);

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Ui.Verify(x => x.ShowLaunchWindow(), Times.Once);
        fixture.Lifetime.Verify(x => x.Shutdown(), Times.Never);
    }

    [Fact]
    public void Start_SecondInstance_SignalsAndShutsDownBeforeCreatingUi()
    {
        var fixture = new BootstrapperFixture();
        fixture.Guard.SetupGet(x => x.IsFirstInstance).Returns(false);
        fixture.Guard.Setup(x => x.SignalActivation());
        fixture.Lifetime.Setup(x => x.Shutdown());

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Guard.Verify(x => x.SignalActivation(), Times.Once);
        fixture.Lifetime.Verify(x => x.Shutdown(), Times.Once);
        fixture.Plugin.Verify(x => x.Initialize(), Times.Never);
        fixture.UiFactory.Verify(x => x(), Times.Never);
    }

    [Fact]
    public void Start_RequiredStartupFailure_LogsAndShutsDown()
    {
        var fixture = new BootstrapperFixture();
        fixture.Guard.SetupGet(x => x.IsFirstInstance).Returns(true);
        fixture.Plugin.Setup(x => x.Initialize()).Throws(new InvalidOperationException("test"));
        fixture.Lifetime.Setup(x => x.Shutdown());

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Lifetime.Verify(x => x.Shutdown(), Times.Once);
        fixture.Preferences.Verify(x => x.Load(), Times.Never);
    }

    [Fact]
    public void Start_OptionalReleaseNotesFailure_ContinuesStartup()
    {
        var fixture = new BootstrapperFixture();
        var preferences = new AppPreferences
        {
            ShowWindowOnStartup = false,
            CheckForUpdatesOnStartup = false
        };

        fixture.Guard.SetupGet(x => x.IsFirstInstance).Returns(true);
        fixture.Plugin.Setup(x => x.Initialize());
        fixture.Preferences.Setup(x => x.Load()).Returns(preferences);
        fixture.StartupPreferences.Setup(x => x.Apply(preferences));
        fixture.UiFactory.Setup(x => x()).Returns(fixture.Ui.Object);
        fixture.DownloadFactory.Setup(x => x()).Returns(fixture.Download.Object);
        fixture.RequestSourceFactory.Setup(x => x()).Returns(fixture.RequestSource.Object);
        fixture.HotkeyFactory.Setup(x => x()).Returns(fixture.Hotkey.Object);
        fixture.Ui.SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object);
        fixture.Hotkey.Setup(x => x.Register(preferences));
        fixture.ReleaseNotes.Setup(x => x.Evaluate(preferences)).Throws(new InvalidOperationException("test"));
        fixture.UpdateStartup
            .Setup(x => x.CheckAsync(false, It.IsAny<Action<string>>()))
            .Returns(Task.CompletedTask);

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Ui.Verify(x => x.ShowLaunchWindow(), Times.Never);
        fixture.Hotkey.Verify(x => x.Register(preferences), Times.Once);
        fixture.UpdateStartup.Verify(x => x.CheckAsync(false, It.IsAny<Action<string>>()), Times.Once);
        fixture.Lifetime.Verify(x => x.Shutdown(), Times.Never);
    }

    [Fact]
    public void AddClIHubServices_RegistersBootstrapperAndGuardPort()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ApplicationBootstrapper>(provider.GetRequiredService<IApplicationBootstrapper>());
        Assert.Same(
            provider.GetRequiredService<SingleInstanceGuard>(),
            provider.GetRequiredService<ISingleInstanceGuard>());
        Assert.Same(
            provider.GetRequiredService<IApplicationOperationLifetime>(),
            provider.GetRequiredService<IApplicationOperationLifetime>());
    }

    private static ApplicationStartupContext CreateContext(IStartupWindow window) =>
        new(_ => { }, _ => { });

    private sealed class BootstrapperFixture
    {
        public Mock<ISingleInstanceGuard> Guard { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationLifetime> Lifetime { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationOperationLifetime> OperationLifetime { get; } = new(MockBehavior.Strict);
        public Mock<IPluginInitializationService> Plugin { get; } = new(MockBehavior.Strict);
        public Mock<IPreferencesStore> Preferences { get; } = new(MockBehavior.Strict);
        public Mock<IStartupPreferencesApplier> StartupPreferences { get; } = new(MockBehavior.Strict);
        public Mock<IHotkeyStartupRegistrar> Hotkey { get; } = new(MockBehavior.Strict);
        public Mock<Func<IHotkeyStartupRegistrar>> HotkeyFactory { get; } = new(MockBehavior.Strict);
        public Mock<IReleaseNotesStartupCoordinator> ReleaseNotes { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateStartupCoordinator> UpdateStartup { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateService> UpdateService { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationStartupUi> Ui { get; } = new(MockBehavior.Strict);
        public Mock<IStartupWindow> Window { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateDownloadCoordinator> Download { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateRequestSource> RequestSource { get; } = new(MockBehavior.Strict);
        public Mock<Func<IApplicationStartupUi>> UiFactory { get; } = new(MockBehavior.Strict);
        public Mock<Func<IUpdateDownloadCoordinator>> DownloadFactory { get; } = new(MockBehavior.Strict);
        public Mock<Func<IUpdateRequestSource>> RequestSourceFactory { get; } = new(MockBehavior.Strict);

        public ApplicationBootstrapper Bootstrapper { get; }

        public BootstrapperFixture()
        {
            OperationLifetime
                .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
                .Returns((string _, Func<CancellationToken, Task> operation) => operation(CancellationToken.None));

            Bootstrapper = new ApplicationBootstrapper(
                Guard.Object,
                Lifetime.Object,
                OperationLifetime.Object,
                Plugin.Object,
                Preferences.Object,
                StartupPreferences.Object,
                HotkeyFactory.Object,
                ReleaseNotes.Object,
                UpdateStartup.Object,
                UpdateService.Object,
                UiFactory.Object,
                DownloadFactory.Object,
                RequestSourceFactory.Object,
                NullLogger<ApplicationBootstrapper>.Instance);
        }
    }
}
