namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Velopack;
using Velopack.Locators;
using Velopack.Sources;

public class UpdateInstallerTests : IDisposable
{
    private readonly string _packagesDir;
    private readonly UpdateInstaller _installer;

    public UpdateInstallerTests()
    {
        _packagesDir = Path.Combine(Path.GetTempPath(), "clihub-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_packagesDir);
        _installer = new UpdateInstaller(NullLogger.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_packagesDir, recursive: true); } catch { }
    }

    [Fact]
    public void ApplyAndRestart_WhenNoAsset_ReturnsNoDownload()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);

        // Act
        var result = _installer.ApplyAndRestart(manager, null);

        // Assert
        Assert.Equal(UpdateInstallStatus.NoDownload, result.Status);
    }

    [Fact]
    public void ApplyAndExit_WhenNoAsset_ReturnsNoDownload()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);

        // Act
        var result = _installer.ApplyAndExit(manager, null);

        // Assert
        Assert.Equal(UpdateInstallStatus.NoDownload, result.Status);
    }

    [Fact]
    public void ApplyAndRestart_WhenAssetProvided_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);

        var asset = CreateFakeAsset("2.0.0");

        // Act
        var result = _installer.ApplyAndRestart(manager, asset);

        // Assert
        // ApplyUpdatesAndRestart will fail in test environment (no real update package)
        // and return Failed status
        Assert.Equal(UpdateInstallStatus.Failed, result.Status);
    }

    [Fact]
    public void ApplyAndExit_WhenAssetProvided_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", _packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(_packagesDir));
        var manager = new UpdateManager(source, null, locator);

        var asset = CreateFakeAsset("2.0.0");

        // Act
        var result = _installer.ApplyAndExit(manager, asset);

        // Assert
        // ApplyUpdatesAndExit will fail in test environment (no real update package)
        // and return Failed status
        Assert.Equal(UpdateInstallStatus.Failed, result.Status);
    }

    private static VelopackAsset CreateFakeAsset(string version)
    {
        return new VelopackAsset
        {
            PackageId = "CLIHub",
            Version = SemanticVersion.Parse(version),
            Type = VelopackAssetType.Full,
            FileName = $"CLIHub-{version}-full.nupkg",
            SHA1 = "fake-sha1",
            Size = 1000
        };
    }
}
