namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Velopack;
using Velopack.Locators;
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

    private UpdateService CreateDefaultService() =>
        new(NullLogger<UpdateService>.Instance);

    private UpdateService CreateInstalledService(string version)
    {
        var locator = new TestVelopackLocator("CLIHub", version, _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);
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
}
