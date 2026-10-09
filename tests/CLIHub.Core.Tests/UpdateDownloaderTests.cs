namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

public class UpdateDownloaderTests : IDisposable
{
    private readonly string _packagesDir;
    private readonly UpdateDownloader _downloader;

    public UpdateDownloaderTests()
    {
        _packagesDir = Path.Combine(Path.GetTempPath(), "clihub-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_packagesDir);
        _downloader = new UpdateDownloader(NullLogger.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_packagesDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task DownloadAsync_WhenNoAsset_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SuccessfulSource();
        var manager = new UpdateManager(source, null, locator);

        // Create update without target asset
        var update = new UpdateInfo(null!, false);

        // Act
        var result = await _downloader.DownloadAsync(manager, update, CancellationToken.None);

        // Assert
        Assert.Equal(UpdateDownloadStatus.Failed, result.Status);
        Assert.Null(result.AvailableVersion);
        Assert.False(_downloader.IsDownloading);
    }

    [Fact]
    public async Task DownloadAsync_WhenConcurrent_ReturnsAlreadyDownloading()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var delayedSource = new DelayedSource(TimeSpan.FromSeconds(2));
        var manager = new UpdateManager(delayedSource, null, locator);

        var update = CreateFakeUpdate("2.0.0");

        // Act - start first download
        var firstTask = _downloader.DownloadAsync(manager, update, CancellationToken.None);

        // Wait a bit to ensure first download started
        await Task.Delay(50);

        // Try second concurrent download
        var result = await _downloader.DownloadAsync(manager, update, CancellationToken.None);

        // Assert
        Assert.Equal(UpdateDownloadStatus.AlreadyDownloading, result.Status);
        Assert.Null(result.AvailableVersion);

        // Cleanup: wait for first download to complete
        await firstTask;
    }

    [Fact]
    public async Task DownloadAsync_WhenCancelled_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var delayedSource = new DelayedSource(TimeSpan.FromSeconds(10));
        var manager = new UpdateManager(delayedSource, null, locator);

        var update = CreateFakeUpdate("2.0.0");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await _downloader.DownloadAsync(manager, update, cts.Token);

        // Assert
        Assert.Equal(UpdateDownloadStatus.Failed, result.Status);
        Assert.Equal("2.0.0", result.AvailableVersion);
        Assert.False(_downloader.IsDownloading);
    }

    [Fact]
    public void IsDownloading_Initially_ReturnsFalse()
    {
        // Assert
        Assert.False(_downloader.IsDownloading);
    }

    [Fact]
    public void DownloadedAsset_Initially_ReturnsNull()
    {
        // Assert
        Assert.Null(_downloader.DownloadedAsset);
    }

    private static UpdateInfo CreateFakeUpdate(string version)
    {
        var asset = new VelopackAsset
        {
            PackageId = "CLIHub",
            Version = SemanticVersion.Parse(version),
            Type = VelopackAssetType.Full,
            FileName = $"CLIHub-{version}-full.nupkg",
            SHA1 = "fake-sha1",
            Size = 1000
        };

        return new UpdateInfo(asset, false);
    }

    /// <summary>
    ///   Test source that successfully completes downloads immediately.
    /// </summary>
    private sealed class SuccessfulSource : IUpdateSource
    {
        public Task<VelopackAssetFeed> GetReleaseFeed(
            IVelopackLogger logger,
            string? appId,
            string channel,
            Guid? stagingId,
            VelopackAsset? latestLocalRelease)
        {
            return Task.FromResult(new VelopackAssetFeed());
        }

        public Task DownloadReleaseEntry(
            IVelopackLogger logger,
            VelopackAsset releaseEntry,
            string localFile,
            Action<int>? progress,
            CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///   Test source that delays downloads.
    /// </summary>
    private sealed class DelayedSource : IUpdateSource
    {
        private readonly TimeSpan _delay;

        public DelayedSource(TimeSpan delay)
        {
            _delay = delay;
        }

        public Task<VelopackAssetFeed> GetReleaseFeed(
            IVelopackLogger logger,
            string? appId,
            string channel,
            Guid? stagingId,
            VelopackAsset? latestLocalRelease)
        {
            return Task.FromResult(new VelopackAssetFeed());
        }

        public async Task DownloadReleaseEntry(
            IVelopackLogger logger,
            VelopackAsset releaseEntry,
            string localFile,
            Action<int>? progress,
            CancellationToken cancelToken)
        {
            await Task.Delay(_delay, cancelToken);
        }
    }
}
