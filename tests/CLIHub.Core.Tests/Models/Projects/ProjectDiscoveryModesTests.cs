namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class ProjectDiscoveryModesTests
{
    [Fact]
    public void ToToken_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProjectDiscoveryModes.ToToken((ProjectDiscoveryMode)42));
    }
}
