using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Tests;

public sealed class PhysicalFileSystemTests
{
    [Fact]
    public void Move_OverwritesExistingTarget()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "source.txt");
        var target = Path.Combine(temp.Path, "target.txt");
        File.WriteAllText(source, "new");
        File.WriteAllText(target, "old");

        new PhysicalFileSystem().Move(source, target, overwrite: true);

        Assert.False(File.Exists(source));
        Assert.Equal("new", File.ReadAllText(target));
    }
}
