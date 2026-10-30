namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Agents;

public class AgentProbePolicyTests
{
    [Fact]
    public void ResolveTtl_WithPositivePreference_ReturnsPreference() =>
        Assert.Equal(TimeSpan.FromMinutes(3), AgentProbePolicy.ResolveTtl(3));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ResolveTtl_WithZeroOrNegative_ReturnsDefault(int minutes) =>
        Assert.Equal(AgentProbePolicy.DefaultTtl, AgentProbePolicy.ResolveTtl(minutes));

    [Fact]
    public void ResolveTtl_WithNull_ReturnsDefault() =>
        Assert.Equal(AgentProbePolicy.DefaultTtl, AgentProbePolicy.ResolveTtl(null));

    [Fact]
    public void ResolveTimeout_WithPositivePreference_ReturnsPreference() =>
        Assert.Equal(TimeSpan.FromSeconds(4), AgentProbePolicy.ResolveTimeout(4));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ResolveTimeout_WithZeroOrNegative_ReturnsDefault(int seconds) =>
        Assert.Equal(AgentProbePolicy.DefaultProbeTimeout, AgentProbePolicy.ResolveTimeout(seconds));

    [Fact]
    public void ResolveTimeout_WithNull_ReturnsDefault() =>
        Assert.Equal(AgentProbePolicy.DefaultProbeTimeout, AgentProbePolicy.ResolveTimeout(null));

    [Fact]
    public void DefaultTtl_Is15Minutes() =>
        Assert.Equal(TimeSpan.FromMinutes(15), AgentProbePolicy.DefaultTtl);

    [Fact]
    public void DefaultProbeTimeout_Is10Seconds() =>
        Assert.Equal(TimeSpan.FromSeconds(10), AgentProbePolicy.DefaultProbeTimeout);
}
