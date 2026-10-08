namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Moq;

public class UpdateWorkflowTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_ConcurrentAndReentrantRequests_ShareCheckAndState()
    {
        var pending = new TaskCompletionSource<UpdateCheckResult>();
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>())).Returns(pending.Task);
        using var workflow = Create(core.Object);
        var states = new List<bool>();
        Task<UpdateCheckResult>? reentrant = null;
        workflow.UpdateStateChanged += (_, _) =>
        {
            states.Add(workflow.IsCheckingForUpdates);
            if (workflow.IsCheckingForUpdates)
            {
                reentrant = workflow.CheckForUpdatesAsync();
            }
        };

        var first = workflow.CheckForUpdatesAsync();
        var second = workflow.CheckForUpdatesAsync();
        Assert.Same(first, second);
        Assert.Same(first, reentrant);
        var result = new UpdateCheckResult(UpdateStatus.UpdateAvailable, "1.0", "2.0");
        pending.SetResult(result);

        Assert.Equal(result, await first);
        Assert.Equal(result, await second);
        Assert.Equal([true, false], states);
        core.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UpdateStatus.UpToDate)]
    [InlineData(UpdateStatus.NotInstalled)]
    [InlineData(UpdateStatus.Failed)]
    public async Task CheckForUpdatesAsync_SynchronousCompletion_AllowsLaterRetry(UpdateStatus status)
    {
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(status, "1.0", null));
        using var workflow = Create(core.Object);

        Assert.Equal(status, (await workflow.CheckForUpdatesAsync()).Status);
        Assert.False(workflow.IsCheckingForUpdates);
        await workflow.CheckForUpdatesAsync();

        core.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Exception_EndsCheckingAndAllowsRetry()
    {
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network"));
        using var workflow = Create(core.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.CheckForUpdatesAsync());
        Assert.False(workflow.IsCheckingForUpdates);
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.CheckForUpdatesAsync());
        core.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CheckForUpdatesAsync_JoiningCallerCancels_DoesNotCancelSharedCheck()
    {
        var pending = new TaskCompletionSource<UpdateCheckResult>();
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>())).Returns(pending.Task);
        using var workflow = Create(core.Object);
        using var cancellation = new CancellationTokenSource();
        var first = workflow.CheckForUpdatesAsync();
        var second = workflow.CheckForUpdatesAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.True(workflow.IsCheckingForUpdates);
        pending.SetResult(new(UpdateStatus.UpToDate, "1.0", null));
        await first;
        Assert.False(workflow.IsCheckingForUpdates);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OwningCallerCancels_ObservesCancellationAndAllowsRetry()
    {
        using var cancellation = new CancellationTokenSource();
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new UpdateCheckResult(UpdateStatus.UpToDate, "1.0", null);
            });
        using var workflow = Create(core.Object);
        var first = workflow.CheckForUpdatesAsync(cancellation.Token);
        var second = workflow.CheckForUpdatesAsync();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.False(workflow.IsCheckingForUpdates);
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0", null));
        Assert.Equal(UpdateStatus.UpToDate, (await workflow.CheckForUpdatesAsync()).Status);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_Success_NotifiesBeforeDelayAndApply()
    {
        var sequence = new List<string>();
        var core = DownloadCore(UpdateDownloadStatus.Downloaded, "2.0");
        core.Setup(x => x.ApplyDownloadedUpdateAndRestart()).Callback(() => sequence.Add("apply"));
        using var workflow = Create(core.Object, (duration, _) =>
        {
            Assert.Equal(TimeSpan.FromSeconds(2), duration);
            sequence.Add("delay");
            return Task.CompletedTask;
        });
        workflow.UpdateDownloaded += (_, version) => sequence.Add($"notify {version}");
        await workflow.DownloadAndApplyAsync();

        Assert.Equal(["notify 2.0", "delay", "apply"], sequence);
        Assert.False(workflow.IsDownloading);
    }

    [Theory]
    [InlineData(UpdateDownloadStatus.AlreadyDownloading)]
    [InlineData(UpdateDownloadStatus.NoUpdate)]
    [InlineData(UpdateDownloadStatus.NotInstalled)]
    [InlineData(UpdateDownloadStatus.Failed)]
    public async Task DownloadAndApplyAsync_NonDownloadedResult_RefreshesWithoutApply(UpdateDownloadStatus status)
    {
        var core = DownloadCore(status);
        using var workflow = Create(core.Object);
        var states = new List<bool>();
        workflow.UpdateStateChanged += (_, _) => states.Add(workflow.IsDownloading);
        workflow.UpdateDownloaded += (_, _) => Assert.Fail("Unexpected success notification.");
        workflow.UpdateDownloadFailed += (_, _) => Assert.Fail("Unknown version must not be notified.");

        await workflow.DownloadAndApplyAsync();

        Assert.Equal([true, false], states);
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_FailedVersion_NotifiesFailure()
    {
        var core = DownloadCore(UpdateDownloadStatus.Failed, "2.0");
        using var workflow = Create(core.Object);
        string? failure = null;
        workflow.UpdateDownloadFailed += (_, version) => failure = version;
        await workflow.DownloadAndApplyAsync();
        Assert.Equal("2.0", failure);
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadAndApplyAsync_DownloadOrApplyThrows_RefreshesWithoutThrowing(bool applyFails)
    {
        var core = DownloadCore(UpdateDownloadStatus.Downloaded, "2.0");
        if (applyFails)
        {
            core.Setup(x => x.ApplyDownloadedUpdateAndRestart()).Throws(new InvalidOperationException("apply"));
        }
        else
        {
            core.Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("download"));
        }

        using var workflow = Create(core.Object);
        await workflow.DownloadAndApplyAsync();
        Assert.False(workflow.IsDownloading);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_DuringNotificationDelay_RejectsSecondDownloadAndHonorsCancellation()
    {
        var core = DownloadCore(UpdateDownloadStatus.Downloaded, "2.0");
        var delayStarted = new TaskCompletionSource();
        using var cancellation = new CancellationTokenSource();
        using var workflow = Create(core.Object, (_, token) =>
        {
            delayStarted.SetResult();
            return Task.Delay(Timeout.Infinite, token);
        });
        var first = workflow.DownloadAndApplyAsync(cancellation.Token);
        await delayStarted.Task;

        Assert.Equal(UpdateDownloadStatus.AlreadyDownloading, (await workflow.DownloadUpdateAsync()).Status);
        await workflow.DownloadAndApplyAsync();
        cancellation.Cancel();
        await first;

        core.Verify(x => x.DownloadUpdateAsync(cancellation.Token), Times.Once);
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
        Assert.False(workflow.IsDownloading);
    }

    [Fact]
    public async Task DownloadUpdateAsync_Success_WaitsForExplicitApply()
    {
        var core = DownloadCore(UpdateDownloadStatus.Downloaded, "2.0");
        using var cancellation = new CancellationTokenSource();
        using var workflow = Create(core.Object);
        Assert.Equal(UpdateDownloadStatus.Downloaded, (await workflow.DownloadUpdateAsync(cancellation.Token)).Status);
        core.Verify(x => x.DownloadUpdateAsync(cancellation.Token), Times.Once);
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
        workflow.ApplyDownloadedUpdateAndRestart();
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Once);
    }

    [Fact]
    public void Dispose_AfterForwardingCoreState_Unsubscribes()
    {
        var core = new Mock<IUpdateService>();
        core.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0");
        using var workflow = Create(core.Object);
        var changes = 0;
        workflow.UpdateStateChanged += (_, _) => changes++;
        core.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);
        Assert.Equal("2.0", workflow.LastKnownAvailableVersion);
        workflow.Dispose();
        core.Raise(x => x.UpdateStateChanged += null, EventArgs.Empty);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void ServiceRegistration_Workflow_IsSingletonAndDoesNotConstructTray()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();
        services.AddSingleton<IUpdateService>(Mock.Of<IUpdateService>());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var workflow = provider.GetRequiredService<IUpdateWorkflow>();
        Assert.IsType<UpdateWorkflow>(workflow);
        Assert.Same(workflow, provider.GetRequiredService<IUpdateWorkflow>());
    }

    private static Mock<IUpdateService> DownloadCore(UpdateDownloadStatus status, string? version = null)
    {
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateDownloadResult(status, version));
        return core;
    }

    internal static UpdateWorkflow Create(
        IUpdateService core,
        Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        new(core, core, core, core, core, NullLogger<UpdateWorkflow>.Instance,
            delay ?? ((_, _) => Task.CompletedTask));
}
