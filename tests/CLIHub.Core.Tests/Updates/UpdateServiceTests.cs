namespace CLIHub.Core.Tests.Updates;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

public class UpdateServiceTests : IDisposable
{
    private readonly string packagesDir;

    public UpdateServiceTests()
    {
        this.packagesDir = Path.Combine(Path.GetTempPath(), "clihub-velopack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.packagesDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.packagesDir, recursive: true); } catch { }
    }

    /// <summary>
    ///   A source whose release feed takes a while to answer, so a download request stays
    ///   in flight long enough to observe the concurrent-request guard.
    /// </summary>
    private sealed class DelayedSource : IUpdateSource
    {
        private readonly TimeSpan delay;
        private readonly bool fail;

        public DelayedSource(TimeSpan delay, bool fail = false)
        {
            this.delay = delay;
            this.fail = fail;
        }

        public async Task<VelopackAssetFeed> GetReleaseFeed(
            IVelopackLogger logger,
            string? appId,
            string channel,
            Guid? stagingId,
            VelopackAsset? latestLocalRelease)
        {
            await Task.Delay(this.delay);
            if (this.fail)
            {
                throw new InvalidOperationException("late update feed failure");
            }

            return new VelopackAssetFeed { Assets = Array.Empty<VelopackAsset>() };
        }

        public Task DownloadReleaseEntry(
            IVelopackLogger logger,
            VelopackAsset releaseEntry,
            string localFile,
            Action<int>? progress,
            CancellationToken cancelToken) => Task.CompletedTask;
    }

    private UpdateService CreateDefaultService() =>
        new(NullLogger<UpdateService>.Instance, new StubPreferencesStore());

    private UpdateService CreateInstalledService(string version)
    {
        var locator = new TestVelopackLocator("CLIHub", version, this.packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(this.packagesDir));
        var manager = new UpdateManager(source, null, locator);
        return UpdateService.CreateForTesting(NullLogger<UpdateService>.Instance, manager);
    }

    private UpdateService CreateDelayedService(TimeSpan delay)
    {
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var manager = new UpdateManager(new DelayedSource(delay), null, locator);
        return UpdateService.CreateForTesting(NullLogger<UpdateService>.Instance, manager);
    }

    private UpdateService CreateDelayedService(TimeSpan delay, TimeSpan checkTimeout, bool fail = false)
    {
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var manager = new UpdateManager(new DelayedSource(delay, fail), null, locator);
        return UpdateService.CreateForTesting(NullLogger<UpdateService>.Instance, manager, checkTimeout);
    }

    [Fact]
    public void GetCurrentVersion_NotInstalled_UsesAssemblyVersion()
    {
        var version = this.CreateDefaultService().GetCurrentVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain("+", version);
    }

    [Fact]
    public async Task CheckForUpdates_WhenNotInstalled_ReturnsNotInstalledWithoutNetwork()
    {
        var result = await this.CreateDefaultService().CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.NotInstalled, result.Status);
        Assert.Null(result.AvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_InstalledWithNoReleases_ReturnsUpToDate()
    {
        var result = await this.CreateInstalledService("1.0.0").CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.UpToDate, result.Status);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Null(result.AvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_NeverThrows()
    {
        var exception = await Record.ExceptionAsync(() => this.CreateDefaultService().CheckForUpdatesAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task CheckForUpdates_TimeoutObservesLateSuccessAndLeavesStateClear()
    {
        var service = this.CreateDelayedService(TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(20));

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Null(service.LastKnownAvailableVersion);
        await Task.Delay(180);
        Assert.Null(service.LastKnownAvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_TimeoutObservesLateFailure()
    {
        var service = this.CreateDelayedService(TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(20), fail: true);

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.Failed, result.Status);
        await Task.Delay(180);
        Assert.Null(service.LastKnownAvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_CancellationObservesLateCompletionAndLeavesStateClear()
    {
        var service = this.CreateDelayedService(TimeSpan.FromMilliseconds(150), TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await service.CheckForUpdatesAsync(cancellation.Token);

        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Null(service.LastKnownAvailableVersion);
    }

    [Fact]
    public void IsDownloading_InitiallyFalse()
    {
        Assert.False(this.CreateDefaultService().IsDownloading);
    }

    [Fact]
    public void LastKnownAvailableVersion_InitiallyNull()
    {
        Assert.Null(this.CreateDefaultService().LastKnownAvailableVersion);
    }

    [Fact]
    public async Task DownloadUpdate_NotInstalled_ReturnsNotInstalled()
    {
        var result = await this.CreateDefaultService().DownloadUpdateAsync();

        Assert.Equal(UpdateDownloadStatus.NotInstalled, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.False(this.CreateDefaultService().IsDownloading);
    }

    [Fact]
    public async Task DownloadUpdate_InstalledWithNoReleases_ReturnsNoUpdate()
    {
        var service = this.CreateInstalledService("1.0.0");

        var result = await service.DownloadUpdateAsync();

        Assert.Equal(UpdateDownloadStatus.NoUpdate, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.Null(service.LastKnownAvailableVersion);
    }

    [Fact]
    public async Task DownloadUpdate_SecondCallWhileRunning_ReturnsAlreadyDownloading()
    {
        var service = this.CreateDelayedService(TimeSpan.FromSeconds(2));

        var first = service.DownloadUpdateAsync();
        var second = await service.DownloadUpdateAsync();

        Assert.Equal(UpdateDownloadStatus.AlreadyDownloading, second.Status);
        Assert.Null(second.AvailableVersion);
        Assert.True(service.IsDownloading);

        var firstResult = await first;
        Assert.Equal(UpdateDownloadStatus.NoUpdate, firstResult.Status);
        Assert.False(service.IsDownloading);
    }

    [Fact]
    public async Task DownloadUpdate_NeverThrows()
    {
        var exception = await Record.ExceptionAsync(() => this.CreateDefaultService().DownloadUpdateAsync());
        Assert.Null(exception);
    }

    [Fact]
    public void ApplyDownloadedUpdateAndRestart_WithoutDownloadedUpdate_DoesNotThrow()
    {
        var exception = Record.Exception(() => this.CreateDefaultService().ApplyDownloadedUpdateAndRestart());
        Assert.Null(exception);
    }

    private sealed class StubPreferencesStore : IPreferencesStore
    {
        public AppPreferences Load() => new();

        public void Update(Action<AppPreferences> update) => update(new AppPreferences());
    }
}
