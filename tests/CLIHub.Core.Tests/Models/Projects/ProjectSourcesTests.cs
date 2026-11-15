namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class ProjectSourcesTests
{
    [Fact]
    public void ToToken_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProjectSources.ToToken((ProjectSource)42));
    }
}
