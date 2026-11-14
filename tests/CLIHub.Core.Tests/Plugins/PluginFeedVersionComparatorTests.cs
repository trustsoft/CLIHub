namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class PluginFeedVersionComparatorTests
{
    [Fact]
    public void Compare_NumericInstalledMatchesFeed_ReturnsCurrent()
    {
        var result = PluginFeedVersionComparator.Compare("1.2.3", "1.2.3");

        Assert.Equal(PluginUpdateStatus.Current, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.2.3")]
    [InlineData("1.2.0", "1.2.3")]
    [InlineData("0.9.0", "1.0.0")]
    public void Compare_NumericInstalledLessThanFeed_ReturnsUpdateAvailable(string installed, string feed)
    {
        var result = PluginFeedVersionComparator.Compare(installed, feed);

        Assert.Equal(PluginUpdateStatus.UpdateAvailable, result);
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0")]
    [InlineData("1.2.3", "1.2.0")]
    [InlineData("1.10.0", "1.9.99")]
    public void Compare_NumericInstalledGreaterThanFeed_ReturnsDifferentVersionAvailable(string installed, string feed)
    {
        var result = PluginFeedVersionComparator.Compare(installed, feed);

        Assert.Equal(PluginUpdateStatus.DifferentVersionAvailable, result);
    }

    [Fact]
    public void Compare_NonNumericInstalledMatchesFeed_ReturnsCurrent()
    {
        var result = PluginFeedVersionComparator.Compare("v1.2.3-beta", "v1.2.3-beta");

        Assert.Equal(PluginUpdateStatus.Current, result);
    }

    [Fact]
    public void Compare_NonNumericInstalledDiffersFeed_ReturnsDifferentVersionAvailable()
    {
        var result = PluginFeedVersionComparator.Compare("v1.2.3-beta", "v1.2.3-rc");

        Assert.Equal(PluginUpdateStatus.DifferentVersionAvailable, result);
    }

    [Fact]
    public void Compare_InstalledNull_ReturnsAvailable()
    {
        var result = PluginFeedVersionComparator.Compare(null, "1.0.0");

        Assert.Equal(PluginUpdateStatus.Available, result);
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0")]
    [InlineData("1.2.3", "1.2.0")]
    [InlineData("1.10.0", "1.9.99")]
    public void IsDowngrade_InstalledNumericallyGreaterThanFeed_ReturnsTrue(string installed, string feed)
    {
        var result = PluginFeedVersionComparator.IsDowngrade(installed, feed);

        Assert.True(result);
    }

    [Fact]
    public void IsDowngrade_InstalledMatchesFeed_ReturnsFalse()
    {
        var result = PluginFeedVersionComparator.IsDowngrade("1.2.3", "1.2.3");

        Assert.False(result);
    }

    [Theory]
    [InlineData("1.0.0", "1.2.3")]
    [InlineData("0.9.0", "1.0.0")]
    public void IsDowngrade_FeedNewerThanInstalled_ReturnsFalse(string installed, string feed)
    {
        var result = PluginFeedVersionComparator.IsDowngrade(installed, feed);

        Assert.False(result);
    }

    [Fact]
    public void IsDowngrade_InstalledVersionNull_ReturnsFalse()
    {
        var result = PluginFeedVersionComparator.IsDowngrade(null, "1.0.0");

        Assert.False(result);
    }

    [Theory]
    [InlineData("abc", "1.0.0")]
    [InlineData("1.0", "1.0.0")]
    [InlineData("1.0.0", "abc")]
    [InlineData("1.0.0", "1.0")]
    public void IsDowngrade_NonParseableVersion_ReturnsFalse(string installed, string feed)
    {
        var result = PluginFeedVersionComparator.IsDowngrade(installed, feed);

        Assert.False(result);
    }
}
