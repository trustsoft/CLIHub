namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

public class UpdateDownloadCoordinatorTests
{
    [Fact]
    public async Task DownloadAndApplyAsync_DownloadedUpdate_NotifiesRefreshesDelaysAndApplies()
    {
        var updates = new Mock<IUpdateService>(MockBehavior.Strict);
        updates
            .Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateDownloadResult(UpdateDownloadStatus.Downloaded, "1.2.0"));
        var notifier = CreateNotifier();
        var applied = false;
        updates.Setup(x => x.ApplyDownloadedUpdateAndRestart()).Callback(() => applied = true);
        var delays = new List<TimeSpan>();
        var coordinator = CreateCoordinator(
            updates.Object,
            notifier.Object,
             (duration, _) =>
            {
                delays.Add(duration);
                return Task.CompletedTask;
            });

        await coordinator.DownloadAndApplyAsync();

        notifier.Verify(x => x.NotifyDownloaded("1.2.0"), Times.Once);
        notifier.Verify(x => x.RefreshMenu(), Times.Once);
        Assert.Equal([TimeSpan.FromSeconds(2)], delays);
        Assert.True(applied);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_FailedDownloadWithVersion_NotifiesAndRefreshes()
    {
        var updates = CreateDownloadService(
            new UpdateDownloadResult(UpdateDownloadStatus.Failed, "1.2.0"));
        var notifier = CreateNotifier();
        var coordinator = CreateCoordinator(updates.Object, notifier.Object, (_, _) => Task.CompletedTask);

        await coordinator.DownloadAndApplyAsync();

        notifier.Verify(x => x.NotifyFailed("1.2.0"), Times.Once);
        notifier.Verify(x => x.RefreshMenu(), Times.Once);
        updates.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
    }

    [Theory]
    [InlineData(UpdateDownloadStatus.AlreadyDownloading)]
    [InlineData(UpdateDownloadStatus.NoUpdate)]
    [InlineData(UpdateDownloadStatus.NotInstalled)]
    [InlineData(UpdateDownloadStatus.Failed)]
    public async Task DownloadAndApplyAsync_NonDownloadedResult_RefreshesWithoutApply(UpdateDownloadStatus status)
    {
        var updates = CreateDownloadService(new UpdateDownloadResult(status, null));
        var notifier = CreateNotifier();
        var coordinator = CreateCoordinator(updates.Object, notifier.Object, (_, _) => Task.CompletedTask);

        await coordinator.DownloadAndApplyAsync();

        notifier.Verify(x => x.RefreshMenu(), Times.Once);
        notifier.Verify(x => x.NotifyDownloaded(It.IsAny<string>()), Times.Never);
        notifier.Verify(x => x.NotifyFailed(It.IsAny<string>()), Times.Never);
        updates.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_WhenDownloadThrows_RefreshesWithoutThrowing()
    {
        var updates = new Mock<IUpdateService>(MockBehavior.Strict);
        updates
            .Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test"));
        var notifier = CreateNotifier();
        var coordinator = CreateCoordinator(updates.Object, notifier.Object, (_, _) => Task.CompletedTask);

        var exception = await Record.ExceptionAsync(() => coordinator.DownloadAndApplyAsync());

        Assert.Null(exception);
        notifier.Verify(x => x.RefreshMenu(), Times.Once);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_ForwardsCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        var updates = new Mock<IUpdateService>(MockBehavior.Strict);
        updates
            .Setup(x => x.DownloadUpdateAsync(cancellation.Token))
            .ReturnsAsync(new UpdateDownloadResult(UpdateDownloadStatus.NoUpdate, null));
        var notifier = CreateNotifier();
        var coordinator = CreateCoordinator(updates.Object, notifier.Object, (_, _) => Task.CompletedTask);

        await coordinator.DownloadAndApplyAsync(cancellation.Token);

        updates.Verify(x => x.DownloadUpdateAsync(cancellation.Token), Times.Once);
    }

    private static Mock<IUpdateService> CreateDownloadService(UpdateDownloadResult result)
    {
        var service = new Mock<IUpdateService>(MockBehavior.Strict);
        service
            .Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return service;
    }

    private static Mock<IUpdateDownloadNotifier> CreateNotifier()
    {
        var notifier = new Mock<IUpdateDownloadNotifier>(MockBehavior.Strict);
        notifier.Setup(x => x.NotifyDownloaded(It.IsAny<string>()));
        notifier.Setup(x => x.NotifyFailed(It.IsAny<string>()));
        notifier.Setup(x => x.RefreshMenu());
        return notifier;
    }

    private static UpdateDownloadCoordinator CreateCoordinator(
        IUpdateService updates,
        IUpdateDownloadNotifier notifier,
        Func<TimeSpan, CancellationToken, Task> delay) =>
        new(updates, notifier, NullLogger<UpdateDownloadCoordinator>.Instance, delay);
}
