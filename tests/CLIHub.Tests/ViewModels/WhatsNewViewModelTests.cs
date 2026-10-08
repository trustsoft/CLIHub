namespace CLIHub.Tests.ViewModels;

using Moq;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

public class WhatsNewViewModelTests
{
    [Fact]
    public void RequestCheckForUpdates_WhenIdle_UsesCheckerAndReturnsToIdle()
    {
        var updates = new Mock<IUpdateService>(MockBehavior.Strict);
        updates.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
        updates.SetupGet(x => x.IsDownloading).Returns(false);
        var checker = new Mock<IUpdateChecker>(MockBehavior.Strict);
        checker
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0.0", null));
        var operationLifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        operationLifetime
            .Setup(x => x.RunAsync("Manual update check", It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) =>
            {
                operation(CancellationToken.None);
                return Task.CompletedTask;
            });
        var releaseNotes = new Mock<IReleaseNotesService>(MockBehavior.Strict);
        releaseNotes.Setup(x => x.GetNotes()).Returns([]);

        var viewModel = new WhatsNewViewModel(
            releaseNotes.Object,
            updates.Object,
            checker.Object,
            operationLifetime.Object);

        viewModel.RequestCheckForUpdates();

        checker.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(viewModel.IsCheckingForUpdates);
        Assert.True(viewModel.CanCheckForUpdates);
    }
}
