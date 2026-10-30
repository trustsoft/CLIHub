namespace CLIHub.Core.Tests.Models.Plugins;

using CLIHub.Core.Models;
using CLIHub.Core.Models.Plugins;

public class UpdateCheckSourceTests
{
    [Fact]
    public void TryResolve_Null_ReturnsFalseWithNone()
    {
        var resolved = UpdateCheckSource.TryResolve(null, out var kind, out var problem);

        Assert.False(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.None, kind);
        Assert.Equal(UpdateCheckSource.Problem.None, problem);
    }

    [Fact]
    public void TryResolve_NpmWithPackage_ReturnsTrueWithNpm()
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck("npm", Package: "@scope/agent"), out var kind, out var problem);

        Assert.True(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.Npm, kind);
        Assert.Equal(UpdateCheckSource.Problem.None, problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_NpmWithBlankPackage_ReturnsFalseWithMissingPackage(string? package)
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck("npm", Package: package), out var kind, out var problem);

        Assert.False(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.None, kind);
        Assert.Equal(UpdateCheckSource.Problem.MissingPackage, problem);
    }

    [Fact]
    public void TryResolve_GitHubReleasesWithRepo_ReturnsTrueWithGitHubReleases()
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck("githubReleases", Repo: "owner/repository"), out var kind, out var problem);

        Assert.True(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.GitHubReleases, kind);
        Assert.Equal(UpdateCheckSource.Problem.None, problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_GitHubReleasesWithBlankRepo_ReturnsFalseWithMissingRepo(string? repo)
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck("githubReleases", Repo: repo), out var kind, out var problem);

        Assert.False(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.None, kind);
        Assert.Equal(UpdateCheckSource.Problem.MissingRepo, problem);
    }

    [Fact]
    public void TryResolve_UnknownSource_ReturnsFalseWithUnknownSource()
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck("pypi", Package: "agent"), out var kind, out var problem);

        Assert.False(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.None, kind);
        Assert.Equal(UpdateCheckSource.Problem.UnknownSource, problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_BlankSource_ReturnsFalseWithUnknownSource(string? source)
    {
        var resolved = UpdateCheckSource.TryResolve(
            new PluginUpdateCheck(source!), out var kind, out var problem);

        Assert.False(resolved);
        Assert.Equal(UpdateCheckSource.SourceKind.None, kind);
        Assert.Equal(UpdateCheckSource.Problem.UnknownSource, problem);
    }
}
