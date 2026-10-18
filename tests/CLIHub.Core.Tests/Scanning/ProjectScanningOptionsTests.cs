namespace CLIHub.Core.Tests.Scanning;

using CLIHub.Core.Models;
using CLIHub.Core.Scanning;

public class ProjectScanningOptionsTests
{
    [Fact]
    public void From_AbsentFields_UsesDefaults()
    {
        var options = ProjectScanningOptions.From(new AppPreferences());

        Assert.Empty(options.WatchedDirectories);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, options.ScanDepth);
        Assert.Equal(ProjectScanningDefaults.IgnoredPatterns, options.IgnoredPatterns);
        Assert.Equal(ProjectDiscoveryMode.Both, options.DiscoveryMode);
    }

    [Fact]
    public void From_ExplicitEmptyPatterns_PreservesEmptyList()
    {
        var options = ProjectScanningOptions.From(new AppPreferences { IgnoredPatterns = [] });

        Assert.Empty(options.IgnoredPatterns);
    }

    [Theory]
    [InlineData(0, ProjectScanningDefaults.MinScanDepth)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(99, ProjectScanningDefaults.MaxScanDepth)]
    public void From_OutOfRangeDepth_IsNormalized(int stored, int expected)
    {
        var options = ProjectScanningOptions.From(new AppPreferences { ScanDepth = stored });

        Assert.Equal(expected, options.ScanDepth);
    }

    [Fact]
    public void ForRoot_UsesStoredSettingsAndFallsBackToTheDefaults()
    {
        var options = new ProjectScanningOptions(
            ["C:\\A", "C:\\B"],
            2,
            [],
            ProjectDiscoveryMode.Both,
            new Dictionary<string, WatchedDirectoryScanSettings>
            {
                ["C:\\A"] = new(5, ProjectDiscoveryMode.Git)
            });

        var specific = options.ForRoot("C:\\A");
        Assert.Equal(5, specific.ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Git, specific.DiscoveryMode);

        var caseInsensitive = options.ForRoot("c:\\a");
        Assert.Equal(5, caseInsensitive.ScanDepth);

        var fallback = options.ForRoot("C:\\B");
        Assert.Equal(2, fallback.ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Both, fallback.DiscoveryMode);
    }
}
