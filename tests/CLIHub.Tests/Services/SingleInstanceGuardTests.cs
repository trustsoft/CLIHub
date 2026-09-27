namespace CLIHub.Tests.Services;

using CLIHub.Core.Services;

public class SingleInstanceGuardTests
{
    [Fact]
    public void FirstGuard_IsFirstInstance()
    {
        using var guard = new SingleInstanceGuard();
        Assert.True(guard.IsFirstInstance);
    }

    [Fact]
    public void SecondGuard_IsNotFirstInstance_WhileFirstHolds()
    {
        using var first = new SingleInstanceGuard();
        using var second = new SingleInstanceGuard();

        Assert.True(first.IsFirstInstance);
        Assert.False(second.IsFirstInstance);
    }
}

public class DirectoryInitializerTests
{
    [Fact]
    public void EnsureAppDataLayout_CreatesFullTree()
    {
        var root = Path.Combine(Path.GetTempPath(), "clihub-dir-" + Guid.NewGuid().ToString("N"));
        try
        {
            DirectoryInitializer.EnsureAppDataLayout(root);

            Assert.True(Directory.Exists(root));
            Assert.True(Directory.Exists(Path.Combine(root, "logs")));
            Assert.True(Directory.Exists(Path.Combine(root, "plugins")));
            Assert.True(Directory.Exists(Path.Combine(root, "cache")));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void EnsureAppDataLayout_IsIdempotent()
    {
        var root = Path.Combine(Path.GetTempPath(), "clihub-dir-" + Guid.NewGuid().ToString("N"));
        try
        {
            DirectoryInitializer.EnsureAppDataLayout(root);
            DirectoryInitializer.EnsureAppDataLayout(root);

            Assert.True(Directory.Exists(Path.Combine(root, "plugins")));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}
