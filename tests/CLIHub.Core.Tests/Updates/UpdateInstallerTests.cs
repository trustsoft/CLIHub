namespace CLIHub.Core.Tests.Updates;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

using Velopack;
using Velopack.Locators;
using Velopack.Sources;

public class UpdateInstallerTests : IDisposable
{
    private readonly string packagesDir;
    private readonly UpdateInstaller installer;

    public UpdateInstallerTests()
    {
        this.packagesDir = Path.Combine(Path.GetTempPath(), "clihub-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.packagesDir);
        this.installer = new UpdateInstaller(NullLogger.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.packagesDir, recursive: true); } catch { }
    }

    [Fact]
    public void ApplyAndRestart_WhenNoAsset_ReturnsNoDownload()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(this.packagesDir));
        var manager = new UpdateManager(source, null, locator);

        // Act
        var result = this.installer.ApplyAndRestart(manager, null);

        // Assert
        Assert.Equal(UpdateInstallStatus.NoDownload, result.Status);
    }

    [Fact]
    public void ApplyAndExit_WhenNoAsset_ReturnsNoDownload()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(this.packagesDir));
        var manager = new UpdateManager(source, null, locator);

        // Act
        var result = this.installer.ApplyAndExit(manager, null);

        // Assert
        Assert.Equal(UpdateInstallStatus.NoDownload, result.Status);
    }

    [Fact]
    public void ApplyAndRestart_WhenAssetProvided_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(this.packagesDir));
        var manager = new UpdateManager(source, null, locator);

        var asset = CreateFakeAsset("2.0.0");

        // Act
        var result = this.installer.ApplyAndRestart(manager, asset);

        // Assert
        // ApplyUpdatesAndRestart will fail in test environment (no real update package)
        // and return Failed status
        Assert.Equal(UpdateInstallStatus.Failed, result.Status);
    }

    [Fact]
    public void ApplyAndExit_WhenAssetProvided_ReturnsFailed()
    {
        // Arrange
        var locator = new TestVelopackLocator("CLIHub", "1.0.0", this.packagesDir);
        var source = new SimpleFileSource(new DirectoryInfo(this.packagesDir));
        var manager = new UpdateManager(source, null, locator);

        var asset = CreateFakeAsset("2.0.0");

        // Act
        var result = this.installer.ApplyAndExit(manager, asset);

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
