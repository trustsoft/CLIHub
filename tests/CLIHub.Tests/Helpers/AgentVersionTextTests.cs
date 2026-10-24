namespace CLIHub.Tests.Helpers;

using CLIHub.Helpers;

using Xunit;

public sealed class AgentVersionTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("…")]
    public void IsPending_SentinelOrMissingValue_ReturnsTrue(string? version)
    {
        Assert.True(AgentVersionText.IsPending(version));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("1.2.3")]
    public void IsPending_ResolvedValue_ReturnsFalse(string? version)
    {
        Assert.False(AgentVersionText.IsPending(version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("…")]
    [InlineData("unknown")]
    public void IsKnown_SentinelOrMissingValue_ReturnsFalse(string? version)
    {
        Assert.False(AgentVersionText.IsKnown(version));
    }

    [Fact]
    public void IsKnown_ResolvedVersion_ReturnsTrue()
    {
        Assert.True(AgentVersionText.IsKnown("1.2.3"));
    }
}
