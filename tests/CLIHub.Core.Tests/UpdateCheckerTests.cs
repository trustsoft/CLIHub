namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

public class UpdateCheckerTests : IDisposable
{
    private readonly string _packagesDir;
    private readonly UpdateChecker _checker;

    public UpdateCheckerTests()
    {
        _packagesDir = Path.Combine(Path.GetTempPath(), "clihub-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_packagesDir);
        _checker = new UpdateChecker(NullLogger.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_packagesDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task CheckAsync_WithNoUpdate_ReturnsUpToDate()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);

        // Act
        var (result, update) = await _checker.CheckAsync(
            manager,
            "1.0.0",
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        // Assert
        Assert.Equal(UpdateStatus.UpToDate, result.Status);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Null(result.AvailableVersion);
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckAsync_WhenTimeout_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var delayedSource = new DelayedSource(TimeSpan.FromSeconds(10), fail: false);
        var manager = new UpdateManager(delayedSource, null, locator);

        // Act
        var (result, update) = await _checker.CheckAsync(
            manager,
            "1.0.0",
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Null(result.AvailableVersion);
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckAsync_WhenCancelled_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var delayedSource = new DelayedSource(TimeSpan.FromSeconds(10), fail: false);
        var manager = new UpdateManager(delayedSource, null, locator);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var (result, update) = await _checker.CheckAsync(
            manager,
            "1.0.0",
            TimeSpan.FromSeconds(30),
            cts.Token);

        // Assert
        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Null(result.AvailableVersion);
        Assert.Null(update);
    }

    /// <summary>
    ///   Test source that delays CheckForUpdatesAsync.
    /// </summary>
    private sealed class DelayedSource : IUpdateSource
    {
        private readonly TimeSpan _delay;
        private readonly bool _fail;

        public DelayedSource(TimeSpan delay, bool fail)
        {
            _delay = delay;
            _fail = fail;
        }

        public async Task<VelopackAssetFeed> GetReleaseFeed(
            IVelopackLogger logger,
            string? appId,
            string channel,
            Guid? stagingId,
            VelopackAsset? latestLocalRelease)
        {
            await Task.Delay(_delay);

            if (_fail)
            {
                throw new InvalidOperationException("Simulated check failure");
            }

            return new VelopackAssetFeed();
        }

        public Task DownloadReleaseEntry(
            IVelopackLogger logger,
            VelopackAsset releaseEntry,
            string localFile,
            Action<int>? progress,
            CancellationToken cancelToken) => Task.CompletedTask;
    }
}
