namespace CLIHub.Tests.Scanning;

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
}
