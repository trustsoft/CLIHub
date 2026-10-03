namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

public class UpdateServiceTests : IDisposable
{
    private readonly string _packagesDir;

    public UpdateServiceTests()
    {
        _packagesDir = Path.Combine(Path.GetTempPath(), "clihub-velopack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_packagesDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_packagesDir, recursive: true); } catch { }
    }

    /// <summary>
    ///   A source whose release feed takes a while to answer, so a download request stays
    ///   in flight long enough to observe the concurrent-request guard.
    /// </summary>
    private sealed class DelayedSource : IUpdateSource
    {
        private readonly TimeSpan _delay;

        public DelayedSource(TimeSpan delay) => _delay = delay;

        public async Task<VelopackAssetFeed> GetReleaseFeed(
            IVelopackLogger logger,
            string? appId,
            string channel,
            Guid? stagingId,
            VelopackAsset? latestLocalRelease)
        {
            await Task.Delay(_delay);
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
        new(NullLogger<UpdateService>.Instance);

    private UpdateService CreateInstalledService(string version)
    {
        var locator = new TestVelopackLocator("CLIHub", version, _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);
        return new UpdateService(NullLogger<UpdateService>.Instance, manager);
    }

    private UpdateService CreateDelayedService(TimeSpan delay)
    {
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var manager = new UpdateManager(new DelayedSource(delay), null, locator);
        return new UpdateService(NullLogger<UpdateService>.Instance, manager);
    }

    [Fact]
    public void GetCurrentVersion_NotInstalled_UsesAssemblyVersion()
    {
        var version = CreateDefaultService().GetCurrentVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain("+", version);
    }

    [Fact]
    public async Task CheckForUpdates_WhenNotInstalled_ReturnsNotInstalledWithoutNetwork()
    {
        var result = await CreateDefaultService().CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.NotInstalled, result.Status);
        Assert.Null(result.AvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_InstalledWithNoReleases_ReturnsUpToDate()
    {
        var result = await CreateInstalledService("1.0.0").CheckForUpdatesAsync();

        Assert.Equal(UpdateStatus.UpToDate, result.Status);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Null(result.AvailableVersion);
    }

    [Fact]
    public async Task CheckForUpdates_NeverThrows()
    {
        var exception = await Record.ExceptionAsync(() => CreateDefaultService().CheckForUpdatesAsync());
        Assert.Null(exception);
    }

    [Fact]
    public void IsDownloading_InitiallyFalse()
    {
        Assert.False(CreateDefaultService().IsDownloading);
    }

    [Fact]
    public void LastKnownAvailableVersion_InitiallyNull()
    {
        Assert.Null(CreateDefaultService().LastKnownAvailableVersion);
    }

    [Fact]
    public async Task DownloadUpdate_NotInstalled_ReturnsNotInstalled()
    {
        var result = await CreateDefaultService().DownloadUpdateAsync();

        Assert.Equal(UpdateDownloadStatus.NotInstalled, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.False(CreateDefaultService().IsDownloading);
    }

    [Fact]
    public async Task DownloadUpdate_InstalledWithNoReleases_ReturnsNoUpdate()
    {
        var service = CreateInstalledService("1.0.0");

        var result = await service.DownloadUpdateAsync();

        Assert.Equal(UpdateDownloadStatus.NoUpdate, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.Null(service.LastKnownAvailableVersion);
    }

    [Fact]
    public async Task DownloadUpdate_SecondCallWhileRunning_ReturnsAlreadyDownloading()
    {
        var service = CreateDelayedService(TimeSpan.FromSeconds(2));

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
        var exception = await Record.ExceptionAsync(() => CreateDefaultService().DownloadUpdateAsync());
        Assert.Null(exception);
    }

    [Fact]
    public void ApplyDownloadedUpdateAndRestart_WithoutDownloadedUpdate_DoesNotThrow()
    {
        var exception = Record.Exception(() => CreateDefaultService().ApplyDownloadedUpdateAndRestart());
        Assert.Null(exception);
    }
}
