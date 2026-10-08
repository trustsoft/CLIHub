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
        var startupState = new StartupState(new AppPreferences
        {
            ShowWindowOnStartup = true,
            CheckForUpdatesOnStartup = true
        });

        fixture.InstanceCoordinator.InSequence(sequence).Setup(x => x.Coordinate()).Returns(InstanceStatus.FirstInstance);
        fixture.StartupStateLoader.InSequence(sequence).Setup(x => x.Load()).Returns(startupState);
        fixture.Session.InSequence(sequence).Setup(x => x.Start(It.IsAny<ApplicationStartupContext>())).Returns(fixture.Ui.Object);
        fixture.HotkeyFactory.InSequence(sequence).Setup(x => x()).Returns(fixture.Hotkey.Object);
        fixture.Ui.InSequence(sequence).SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object);
        fixture.Ui.Setup(x => x.ShowLaunchWindow());
        fixture.Hotkey.InSequence(sequence).Setup(x => x.Register(startupState.Preferences));
        fixture.OptionalStartup.InSequence(sequence).Setup(x => x.RunOptionalStartup(startupState, fixture.Ui.Object, It.IsAny<ApplicationStartupContext>()));

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Ui.Verify(x => x.ShowLaunchWindow(), Times.Once);
    }

    [Fact]
    public void Start_SecondInstance_ReturnsEarlyBeforeCreatingUi()
    {
        var fixture = new BootstrapperFixture();
        fixture.InstanceCoordinator.Setup(x => x.Coordinate()).Returns(InstanceStatus.SecondInstance);

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.StartupStateLoader.Verify(x => x.Load(), Times.Never);
        fixture.Session.Verify(x => x.Start(It.IsAny<ApplicationStartupContext>()), Times.Never);
    }

    [Fact]
    public void Start_RequiredStartupFailure_LogsAndThrows()
    {
        var fixture = new BootstrapperFixture();
        fixture.InstanceCoordinator.Setup(x => x.Coordinate()).Returns(InstanceStatus.FirstInstance);
        fixture.StartupStateLoader.Setup(x => x.Load()).Throws(new InvalidOperationException("test"));

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object)));

        fixture.Session.Verify(x => x.Start(It.IsAny<ApplicationStartupContext>()), Times.Never);
    }

    [Fact]
    public void Start_OptionalReleaseNotesFailure_ContinuesStartup()
    {
        var fixture = new BootstrapperFixture();
        var startupState = new StartupState(new AppPreferences
        {
            ShowWindowOnStartup = false,
            CheckForUpdatesOnStartup = false
        });

        fixture.InstanceCoordinator.Setup(x => x.Coordinate()).Returns(InstanceStatus.FirstInstance);
        fixture.StartupStateLoader.Setup(x => x.Load()).Returns(startupState);
        fixture.Session.Setup(x => x.Start(It.IsAny<ApplicationStartupContext>())).Returns(fixture.Ui.Object);
        fixture.HotkeyFactory.Setup(x => x()).Returns(fixture.Hotkey.Object);
        fixture.Ui.SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object);
        fixture.Hotkey.Setup(x => x.Register(startupState.Preferences));
        fixture.OptionalStartup.Setup(x => x.RunOptionalStartup(startupState, fixture.Ui.Object, It.IsAny<ApplicationStartupContext>()));

        fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object));

        fixture.Ui.Verify(x => x.ShowLaunchWindow(), Times.Never);
        fixture.Hotkey.Verify(x => x.Register(startupState.Preferences), Times.Once);
        fixture.OptionalStartup.Verify(x => x.RunOptionalStartup(startupState, fixture.Ui.Object, It.IsAny<ApplicationStartupContext>()), Times.Once);
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
        public Mock<IInstanceCoordinator> InstanceCoordinator { get; } = new(MockBehavior.Strict);
        public Mock<IStartupStateLoader> StartupStateLoader { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationSession> Session { get; } = new(MockBehavior.Strict);
        public Mock<IHotkeyStartupRegistrar> Hotkey { get; } = new(MockBehavior.Strict);
        public Mock<Func<IHotkeyStartupRegistrar>> HotkeyFactory { get; } = new(MockBehavior.Strict);
        public Mock<IOptionalStartupCoordinator> OptionalStartup { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationStartupUi> Ui { get; } = new(MockBehavior.Strict);
        public Mock<IStartupWindow> Window { get; } = new(MockBehavior.Strict);

        public ApplicationBootstrapper Bootstrapper { get; }

        public BootstrapperFixture()
        {
            Bootstrapper = new ApplicationBootstrapper(
                InstanceCoordinator.Object,
                StartupStateLoader.Object,
                Session.Object,
                HotkeyFactory.Object,
                OptionalStartup.Object,
                NullLogger<ApplicationBootstrapper>.Instance);
        }
    }
}
