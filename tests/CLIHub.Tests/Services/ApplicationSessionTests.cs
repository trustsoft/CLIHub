namespace CLIHub.Tests.Services;

using Moq;

using CLIHub;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;

public class ApplicationSessionTests
{
    [Fact]
    public void Start_WiresApplicationEventsToSessionWorkflows()
    {
        var fixture = new SessionFixture();
        var dispatched = 0;

        fixture.Session.Start(new ApplicationStartupContext(
            action =>
            {
                dispatched++;
                action();
            },
            _ => { }));

        fixture.UpdateDownload.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);
        fixture.StartupUi.Raise(x => x.UpdateDownloadRequested += null, EventArgs.Empty);
        fixture.UpdateRequests.Raise(x => x.UpdateRequested += null, EventArgs.Empty);
        fixture.Guard.Raise(x => x.ActivationRequested += null);

        Assert.Equal(2, dispatched);
        fixture.StartupUi.Verify(x => x.RefreshMenu(), Times.Once);
        fixture.StartupUi.Verify(x => x.ShowLaunchWindow(), Times.Once);
        fixture.OperationLifetime.Verify(
            x => x.RunAsync("Update download", It.IsAny<Func<CancellationToken, Task>>()),
            Times.Exactly(2));
        fixture.UpdateDownload.Verify(x => x.DownloadAndApplyAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void Dispose_UnsubscribesApplicationEvents()
    {
        var fixture = new SessionFixture();
        var dispatched = 0;

        fixture.Session.Start(new ApplicationStartupContext(_ => dispatched++, _ => { }));
        fixture.Session.Dispose();

        fixture.UpdateDownload.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);
        fixture.UpdateDownload.Raise(x => x.UpdateDownloaded += null, fixture.UpdateDownload.Object, "2.0");
        fixture.UpdateDownload.Raise(x => x.UpdateDownloadFailed += null, fixture.UpdateDownload.Object, "2.0");
        fixture.StartupUi.Raise(x => x.UpdateDownloadRequested += null, EventArgs.Empty);
        fixture.UpdateRequests.Raise(x => x.UpdateRequested += null, EventArgs.Empty);
        fixture.Guard.Raise(x => x.ActivationRequested += null);

        Assert.Equal(0, dispatched);
        fixture.OperationLifetime.Verify(
            x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()),
            Times.Never);
        fixture.StartupUi.Verify(x => x.RefreshMenu(), Times.Never);
        fixture.StartupUi.Verify(x => x.ShowLaunchWindow(), Times.Never);
    }

    [Fact]
    public void DownloadOutcomes_ActiveSession_DispatchesTrayNotifications()
    {
        var fixture = new SessionFixture();
        fixture.StartupUi.Setup(x => x.NotifyUpdateDownloaded("2.0"));
        fixture.StartupUi.Setup(x => x.NotifyUpdateFailed("2.0"));
        var dispatched = 0;
        fixture.Session.Start(new ApplicationStartupContext(action =>
        {
            dispatched++;
            action();
        }, _ => { }));

        fixture.UpdateDownload.Raise(x => x.UpdateDownloaded += null, fixture.UpdateDownload.Object, "2.0");
        fixture.UpdateDownload.Raise(x => x.UpdateDownloadFailed += null, fixture.UpdateDownload.Object, "2.0");

        Assert.Equal(2, dispatched);
        fixture.StartupUi.Verify(x => x.NotifyUpdateDownloaded("2.0"), Times.Once);
        fixture.StartupUi.Verify(x => x.NotifyUpdateFailed("2.0"), Times.Once);
    }

    private sealed class SessionFixture
    {
        public Mock<IApplicationOperationLifetime> OperationLifetime { get; } = new(MockBehavior.Strict);
        public Mock<IApplicationStartupUi> StartupUi { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateWorkflow> UpdateDownload { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateRequestSource> UpdateRequests { get; } = new(MockBehavior.Strict);
        public Mock<ISingleInstanceGuard> Guard { get; } = new(MockBehavior.Strict);
        public ApplicationSession Session { get; }

        public SessionFixture()
        {
            StartupUi.Setup(x => x.RefreshMenu());
            StartupUi.Setup(x => x.ShowLaunchWindow());
            OperationLifetime
                .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
                .Returns((string _, Func<CancellationToken, Task> operation) =>
                {
                    operation(CancellationToken.None);
                    return Task.CompletedTask;
                });
            UpdateDownload
                .Setup(x => x.DownloadAndApplyAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Session = new ApplicationSession(
                OperationLifetime.Object,
                () => StartupUi.Object,
                () => UpdateRequests.Object,
                Guard.Object,
                UpdateDownload.Object);
        }
    }
}
