using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class RuntimeResolverTests
{
    private readonly RuntimeResolver _resolver = new();

    [Fact]
    public void Resolve_ActionOverride_Wins()
    {
        var runtime = _resolver.Resolve(new AgentActionSpec { Runtime = "wt" }, "ps");

        Assert.Equal("wt", runtime);
    }

    [Fact]
    public void Resolve_NoActionOverride_UsesConfig()
    {
        var runtime = _resolver.Resolve(new AgentActionSpec(), "ps");

        Assert.Equal("ps", runtime);
    }

    [Fact]
    public void Resolve_NeitherSet_UsesDefault()
    {
        var runtime = _resolver.Resolve(new AgentActionSpec(), null);

        Assert.Equal(RuntimeResolver.DefaultRuntime, runtime);
    }

    [Theory]
    [InlineData("PS")]
    [InlineData("  ps  ")]
    public void Resolve_NormalizesValue(string runtime)
    {
        Assert.Equal("ps", _resolver.Resolve(new AgentActionSpec { Runtime = runtime }, null));
    }
}
