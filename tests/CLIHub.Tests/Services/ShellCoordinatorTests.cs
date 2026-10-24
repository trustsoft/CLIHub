namespace CLIHub.Tests.Services;

using System.Windows;

using Microsoft.Win32;

using Moq;

using CLIHub.Core.Models;
using CLIHub.Core.Windows;
using CLIHub.Startup;
using CLIHub.Updates;
using CLIHub.Lifetime;
using CLIHub.Themes;

public class ShellCoordinatorTests
{
    [Fact]
    public void Constructor_InitializesStartupUi_AndWiresEvents()
    {
        var fixture = new CoordinatorFixture();

        Assert.NotNull(fixture.Coordinator.StartupUi);
        Assert.Same(fixture.StartupUi.Object, fixture.Coordinator.StartupUi);
    }

    [Fact]
    public void Constructor_StartsThemeFollower()
    {
        var fixture = new CoordinatorFixture();

        Assert.Single(fixture.ThemeFollowerSubscribed);
        Assert.Empty(fixture.ThemeFollowerUnsubscribed);
    }

    [Fact]
    public void UpdateStateChanged_DispatchesRefreshMenu()
    {
        var fixture = new CoordinatorFixture();
        var dispatched = 0;
        fixture.SetupDispatch(action =>
        {
            dispatched++;
            action();
        });

        fixture.UpdateWorkflow.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);

        Assert.Equal(1, dispatched);
        fixture.StartupUi.Verify(x => x.RefreshMenu(), Times.Once);
    }

    [Fact]
    public void StartupUi_UpdateDownloadRequested_StartsDownloadWorkflow()
    {
        var fixture = new CoordinatorFixture();

        fixture.StartupUi.Raise(x => x.UpdateDownloadRequested += null, EventArgs.Empty);

        fixture.OperationLifetime.Verify(
            x => x.RunAsync("Update download", It.IsAny<Func<CancellationToken, Task>>()),
            Times.Once);
        fixture.UpdateWorkflow.Verify(x => x.DownloadAndApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void UpdateRequestSource_UpdateRequested_StartsDownloadWorkflow()
    {
        var fixture = new CoordinatorFixture();

        fixture.UpdateRequestSource.Raise(x => x.UpdateRequested += null, EventArgs.Empty);

        fixture.OperationLifetime.Verify(
            x => x.RunAsync("Update download", It.IsAny<Func<CancellationToken, Task>>()),
            Times.Once);
        fixture.UpdateWorkflow.Verify(x => x.DownloadAndApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ActivationRequested_DispatchesShowLaunchWindow()
    {
        var fixture = new CoordinatorFixture();
        var dispatched = 0;
        fixture.SetupDispatch(action =>
        {
            dispatched++;
            action();
        });

        fixture.Guard.Raise(x => x.ActivationRequested += null);

        Assert.Equal(1, dispatched);
        fixture.StartupUi.Verify(x => x.ShowLaunchWindow(), Times.Once);
    }

    [Fact]
    public void UpdateDownloaded_DispatchesNotification()
    {
        var fixture = new CoordinatorFixture();
        fixture.StartupUi.Setup(x => x.NotifyUpdateDownloaded("2.0"));
        var dispatched = 0;
        fixture.SetupDispatch(action =>
        {
            dispatched++;
            action();
        });

        fixture.UpdateWorkflow.Raise(x => x.UpdateDownloaded += null, fixture.UpdateWorkflow.Object, "2.0");

        Assert.Equal(1, dispatched);
        fixture.StartupUi.Verify(x => x.NotifyUpdateDownloaded("2.0"), Times.Once);
    }

    [Fact]
    public void UpdateDownloadFailed_DispatchesNotification()
    {
        var fixture = new CoordinatorFixture();
        fixture.StartupUi.Setup(x => x.NotifyUpdateFailed("2.0"));
        var dispatched = 0;
        fixture.SetupDispatch(action =>
        {
            dispatched++;
            action();
        });

        fixture.UpdateWorkflow.Raise(x => x.UpdateDownloadFailed += null, fixture.UpdateWorkflow.Object, "2.0");

        Assert.Equal(1, dispatched);
        fixture.StartupUi.Verify(x => x.NotifyUpdateFailed("2.0"), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesAllEvents_AndDisposesStartupUi()
    {
        var fixture = new CoordinatorFixture();
        var dispatched = 0;
        fixture.SetupDispatch(_ => dispatched++);

        fixture.Coordinator.Dispose();

        fixture.UpdateWorkflow.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);
        fixture.StartupUi.Raise(x => x.UpdateDownloadRequested += null, EventArgs.Empty);
        fixture.UpdateRequestSource.Raise(x => x.UpdateRequested += null, EventArgs.Empty);
        fixture.Guard.Raise(x => x.ActivationRequested += null);
        fixture.UpdateWorkflow.Raise(x => x.UpdateDownloaded += null, fixture.UpdateWorkflow.Object, "2.0");
        fixture.UpdateWorkflow.Raise(x => x.UpdateDownloadFailed += null, fixture.UpdateWorkflow.Object, "2.0");

        Assert.Equal(0, dispatched);
        fixture.OperationLifetime.Verify(
            x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()),
            Times.Never);
        fixture.StartupUi.Verify(x => x.RefreshMenu(), Times.Never);
        fixture.StartupUi.Verify(x => x.ShowLaunchWindow(), Times.Never);
        fixture.StartupUi.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_DisposesThemeFollower()
    {
        var fixture = new CoordinatorFixture();
        var dispatched = 0;
        fixture.SetupDispatch(_ => dispatched++);
        var handler = Assert.Single(fixture.ThemeFollowerSubscribed);

        fixture.Coordinator.Dispose();

        Assert.Same(handler, Assert.Single(fixture.ThemeFollowerUnsubscribed));

        fixture.ThemeFollowerSystemLight = true;
        fixture.RaisePreferenceChanged();

        Assert.Equal(0, dispatched);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var fixture = new CoordinatorFixture();

        fixture.Coordinator.Dispose();
        fixture.Coordinator.Dispose();

        fixture.StartupUi.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void StartupUi_AfterDispose_ThrowsObjectDisposedException()
    {
        var fixture = new CoordinatorFixture();
        fixture.Coordinator.Dispose();

        Assert.Throws<ObjectDisposedException>(() => fixture.Coordinator.StartupUi);
    }

    private sealed class CoordinatorFixture
    {
        public Mock<IApplicationOperationLifetime> OperationLifetime { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationStartupUi> StartupUi { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateWorkflow> UpdateWorkflow { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateRequestSource> UpdateRequestSource { get; } = new(MockBehavior.Strict);
        public Mock<ISingleInstanceGuard> Guard { get; } = new(MockBehavior.Strict);
        public List<UserPreferenceChangedEventHandler> ThemeFollowerSubscribed { get; } = [];
        public List<UserPreferenceChangedEventHandler> ThemeFollowerUnsubscribed { get; } = [];
        public ShellCoordinator Coordinator { get; }

        private UserPreferenceChangedEventHandler? themeFollowerHandler;
        private Action<Action>? dispatchAction;

        public CoordinatorFixture()
        {
            this.StartupUi.Setup(x => x.RefreshMenu());
            this.StartupUi.Setup(x => x.ShowLaunchWindow());
            this.StartupUi.As<IDisposable>().Setup(x => x.Dispose());
            this.OperationLifetime
                .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
                .Returns((string _, Func<CancellationToken, Task> operation) =>
                {
                    operation(CancellationToken.None);
                    return Task.CompletedTask;
                });
            this.UpdateWorkflow
                .Setup(x => x.DownloadAndApplyAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var context = new ApplicationStartupContext(
                action =>
                {
                    if (this.dispatchAction is not null)
                    {
                        this.dispatchAction(action);
                    }
                },
                _ => { });

            this.Coordinator = new ShellCoordinator(
                this.OperationLifetime.Object,
                () => this.StartupUi.Object,
                () => this.UpdateRequestSource.Object,
                this.Guard.Object,
                this.UpdateWorkflow.Object,
                new SystemThemeFollower(
                    new PaletteSwitcher(_ => new ResourceDictionary()),
                    () => AppThemes.SystemToken,
                    () => this.ThemeFollowerSystemLight,
                    handler =>
                    {
                        this.ThemeFollowerSubscribed.Add(handler);
                        this.themeFollowerHandler = handler;
                    },
                    handler =>
                    {
                        this.ThemeFollowerUnsubscribed.Add(handler);
                        this.themeFollowerHandler = null;
                    }),
                context);
        }

        public bool ThemeFollowerSystemLight { get; set; }

        public void SetupDispatch(Action<Action> dispatchAction)
        {
            this.dispatchAction = dispatchAction;
        }

        /// <summary>
        ///   Raises the personalization broadcast like the static system event would: only the
        ///   currently subscribed handler is invoked, none after an unsubscribe.
        /// </summary>
        public void RaisePreferenceChanged() =>
            this.themeFollowerHandler?.Invoke(this, new UserPreferenceChangedEventArgs(UserPreferenceCategory.General));
    }
}
