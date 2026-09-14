using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class LogoResolverTests
{
    private readonly LogoResolver _resolver = new(new PhysicalFileSystem());

    // Chain

    [Fact]
    public void Resolve_LogoBeatsIcon()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "logo.png"), "x");
        File.WriteAllText(Path.Combine(temp.Path, "icon.png"), "x");

        var resolved = _resolver.Resolve(temp.Path);

        Assert.Equal(Path.Combine(temp.Path, "logo.png"), resolved);
    }

    [Fact]
    public void Resolve_PngPrecedesIcoWithinName()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "logo.png"), "x");
        File.WriteAllText(Path.Combine(temp.Path, "logo.ico"), "x");

        var resolved = _resolver.Resolve(temp.Path);

        Assert.Equal(Path.Combine(temp.Path, "logo.png"), resolved);
    }

    [Fact]
    public void Resolve_IcoOnly_UsesIco()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "logo.ico"), "x");

        var resolved = _resolver.Resolve(temp.Path);

        Assert.Equal(Path.Combine(temp.Path, "logo.ico"), resolved);
    }

    [Fact]
    public void Resolve_FallbackToFavicon()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "favicon.ico"), "x");

        var resolved = _resolver.Resolve(temp.Path);

        Assert.Equal(Path.Combine(temp.Path, "favicon.ico"), resolved);
    }

    [Fact]
    public void Resolve_NothingFound_ReturnsNull()
    {
        using var temp = new TempDirectory();

        Assert.Null(_resolver.Resolve(temp.Path));
    }

    [Fact]
    public void Resolve_EmptyRoot_ReturnsNull()
    {
        Assert.Null(_resolver.Resolve(""));
    }

    // Project override

    [Fact]
    public void Project_AbsoluteOverrideWins()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "logo.png"), "x");
        var custom = Path.Combine(temp.Path, "custom.png");
        File.WriteAllText(custom, "x");
        var project = new ProjectConfig { Path = projectPath, Logo = custom };

        var resolved = _resolver.ResolveProject(project);

        Assert.Equal(custom, resolved);
    }

    [Fact]
    public void Project_RelativeOverrideResolvesAgainstProjectRoot()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
        var brand = Path.Combine(projectPath, "assets", "brand.png");
        File.WriteAllText(brand, "x");
        var project = new ProjectConfig { Path = projectPath, Logo = "assets/brand.png" };

        var resolved = _resolver.ResolveProject(project);

        Assert.Equal(brand, resolved);
    }

    [Fact]
    public void Project_BrokenOverrideFallsBackToChain()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "logo.png"), "x");
        var project = new ProjectConfig { Path = projectPath, Logo = "missing.png" };

        var resolved = _resolver.ResolveProject(project);

        Assert.Equal(Path.Combine(projectPath, "logo.png"), resolved);
    }

    [Fact]
    public void Project_NoOverride_UsesChain()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "icon.png"), "x");
        var project = new ProjectConfig { Path = projectPath, Logo = null };

        var resolved = _resolver.ResolveProject(project);

        Assert.Equal(Path.Combine(projectPath, "icon.png"), resolved);
    }
}
