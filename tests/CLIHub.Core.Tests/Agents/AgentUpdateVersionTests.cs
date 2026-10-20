namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Agents;

public class AgentUpdateVersionTests
{
    [Theory]
    [InlineData("opencode v2.0.32", 2, 0, 32)]
    [InlineData("0.30.0 (OpenClaude)", 0, 30, 0)]
    [InlineData("1.0.100", 1, 0, 100)]
    public void TryExtract_VersionTextWithSurroundingContent_ExtractsFirstNumericTriple(
        string value,
        int major,
        int minor,
        int patch)
    {
        var extracted = AgentUpdateVersion.TryExtract(value, out var triple);

        Assert.True(extracted);
        Assert.Equal(new AgentVersionTriple(major, minor, patch), triple);
    }

    [Theory]
    [InlineData("no version here")]
    [InlineData("")]
    [InlineData("1.2 beta")]
    public void TryExtract_ValueWithoutNumericTriple_ReturnsFalse(string value)
    {
        var extracted = AgentUpdateVersion.TryExtract(value, out var triple);

        Assert.False(extracted);
        Assert.Equal(default, triple);
    }

    [Fact]
    public void TryExtract_NullValue_ReturnsFalse()
    {
        var extracted = AgentUpdateVersion.TryExtract(null, out _);

        Assert.False(extracted);
    }

    [Fact]
    public void TryExtract_PreReleaseSuffix_IsIgnored()
    {
        var extracted = AgentUpdateVersion.TryExtract("2.0.33-beta.1+build.42", out var triple);

        Assert.True(extracted);
        Assert.Equal(new AgentVersionTriple(2, 0, 33), triple);
    }

    [Fact]
    public void Compare_LatestGreaterThanInstalled_IsNegative()
    {
        AgentUpdateVersion.TryExtract("2.0.32", out var installed);
        AgentUpdateVersion.TryExtract("2.0.33", out var latest);

        var comparison = AgentUpdateVersion.Compare(installed, latest);

        Assert.True(comparison < 0);
    }

    [Fact]
    public void Compare_EqualTriples_IsZero()
    {
        AgentUpdateVersion.TryExtract("2.0.32", out var installed);
        AgentUpdateVersion.TryExtract("v2.0.32", out var latest);

        var comparison = AgentUpdateVersion.Compare(installed, latest);

        Assert.Equal(0, comparison);
    }

    [Fact]
    public void Compare_InstalledNewerThanLatest_IsPositive()
    {
        AgentUpdateVersion.TryExtract("1.0.100", out var installed);
        AgentUpdateVersion.TryExtract("1.0.99", out var latest);

        var comparison = AgentUpdateVersion.Compare(installed, latest);

        Assert.True(comparison > 0);
    }

    [Fact]
    public void Compare_TwoDigitMinorAgainstSingleDigitMajorPrefix_ComparesNumericallyNotLexically()
    {
        AgentUpdateVersion.TryExtract("1.2.3", out var installed);
        AgentUpdateVersion.TryExtract("1.10.0", out var latest);

        var comparison = AgentUpdateVersion.Compare(installed, latest);

        // Lexical comparison would put "1.10.0" before "1.2.3"; numeric comparison must not.
        Assert.True(comparison < 0);
    }
}
