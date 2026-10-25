namespace CLIHub.Tests.Helpers;

using CLIHub.Helpers;

using Xunit;

public sealed class AgentVersionTextTests
{
    [Fact]
    public void Detecting_Constant_HoldsTheDetectingChipText()
    {
        Assert.Equal("Detecting…", AgentVersionText.Detecting);
    }

    [Fact]
    public void NotInstalled_Constant_HoldsTheNotInstalledChipText()
    {
        Assert.Equal("Not installed", AgentVersionText.NotInstalled);
    }

    [Fact]
    public void Installed_Constant_HoldsTheInstalledChipText()
    {
        Assert.Equal("Installed", AgentVersionText.Installed);
    }
}
