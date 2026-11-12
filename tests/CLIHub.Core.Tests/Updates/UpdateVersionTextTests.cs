namespace CLIHub.Core.Tests.Updates;

using CLIHub.Core.Updates;

public class UpdateVersionTextTests
{
    [Fact]
    public void StripBuildMetadata_VersionWithCommitSuffix_StripsSuffix()
    {
        Assert.Equal("1.2.3", UpdateVersionText.StripBuildMetadata("1.2.3+a1b2c3"));
    }

    [Fact]
    public void StripBuildMetadata_PlainVersion_ReturnsUnchanged()
    {
        Assert.Equal("1.2.3", UpdateVersionText.StripBuildMetadata("1.2.3"));
    }

    [Fact]
    public void StripBuildMetadata_TrailingPlus_ReturnsCoreVersion()
    {
        Assert.Equal("1.2.3", UpdateVersionText.StripBuildMetadata("1.2.3+"));
    }
}
