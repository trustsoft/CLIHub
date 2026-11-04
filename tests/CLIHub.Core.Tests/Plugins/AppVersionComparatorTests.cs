namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class AppVersionComparatorTests
{
    [Theory]
    [InlineData("1.2.0", "1.1.0", true)]
    [InlineData("1.1.0", "1.1.0", true)]
    [InlineData("1.1.1", "1.1.0", true)]
    [InlineData("2.0.0", "1.99.99", true)]
    public void IsSatisfied_CurrentGreaterOrEqual_ReturnsTrue(string current, string required, bool expected)
    {
        var result = AppVersionComparator.IsSatisfied(current, required);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.1.0", false)]
    [InlineData("1.1.0", "1.1.1", false)]
    [InlineData("0.9.99", "1.0.0", false)]
    public void IsSatisfied_CurrentLessThan_ReturnsFalse(string current, string required, bool expected)
    {
        var result = AppVersionComparator.IsSatisfied(current, required);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSatisfied_MajorVersion_TakesPrecedence()
    {
        var result = AppVersionComparator.IsSatisfied("2.0.0", "1.99.99");

        Assert.True(result);
    }
}
