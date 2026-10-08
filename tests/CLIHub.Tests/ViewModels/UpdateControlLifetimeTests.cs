namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

public class UpdateControlLifetimeTests
{
    [Fact]
    public void Dispose_UnsubscribesFromUpdateStateChanges()
    {
        var updates = new Mock<IUpdateService>(MockBehavior.Strict);
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        updates.SetupGet(x => x.IsDownloading).Returns(false);
        updates.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
        var control = new UpdateControlViewModel(
            updates.Object,
            updates.Object,
            updates.Object,
            updates.Object,
            updates.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            new Mock<IApplicationOperationLifetime>().Object);
        control.Dispose();

        updates.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);

        Assert.Equal("1.0.0", control.UpdateButtonText);
    }

    [Fact]
    public void UpdateCommand_WhenApplicationCancels_ReturnsToIdleWithoutFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var updates = new Mock<IUpdateService>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        updates
            .Setup(x => x.CheckForUpdatesAsync(cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var lifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        lifetime
            .Setup(x => x.RunAsync("Update control", It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(cancellation.Token));

        var viewModel = new UpdateControlViewModel(
            updates.Object,
            updates.Object,
            updates.Object,
            updates.Object,
            updates.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            lifetime.Object);

        viewModel.UpdateControlCommand.Execute(null);

        Assert.Equal("1.0.0", viewModel.UpdateButtonText);
    }
}
