namespace CLIHub.Tests.Scanning;

using CLIHub.Core.Scanning;

public class IgnoredPatternMatcherTests
{
    [Theory]
    [InlineData("node_modules", "node_modules", true)]
    [InlineData("Node_Modules", "node_modules", true)]
    [InlineData("bin", "node_modules", false)]
    [InlineData("obj", "obj", true)]
    public void IsIgnored_ExactName_MatchesCaseInsensitively(string name, string pattern, bool expected) =>
        Assert.Equal(expected, IgnoredPatternMatcher.IsIgnored(name, [pattern]));

    [Theory]
    [InlineData("my-cache-dir", "*cache*", true)]
    [InlineData("cache", "*cache*", true)]
    [InlineData("mycache", "*cache", true)]
    [InlineData("dist-old", "dist*", true)]
    [InlineData("old-dist", "*dist", true)]
    [InlineData("build", "dist*", false)]
    public void IsIgnored_Wildcard_MatchesAsExpected(string name, string pattern, bool expected) =>
        Assert.Equal(expected, IgnoredPatternMatcher.IsIgnored(name, [pattern]));

    [Fact]
    public void IsIgnored_BlankPatterns_AreSkipped()
    {
        Assert.False(IgnoredPatternMatcher.IsIgnored("bin", ["", "  "]));
    }

    [Fact]
    public void IsIgnored_NoPatterns_ReturnsFalse()
    {
        Assert.False(IgnoredPatternMatcher.IsIgnored("bin", []));
    }
}
