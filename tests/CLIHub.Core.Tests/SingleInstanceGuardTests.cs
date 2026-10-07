namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Windows;

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

    [Fact]
    public void ServiceProviderDisposal_ReleasesFirstInstanceMutex()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SingleInstanceGuard>();

        using (var provider = services.BuildServiceProvider())
        {
            Assert.True(provider.GetRequiredService<SingleInstanceGuard>().IsFirstInstance);
        }

        using var replacement = new SingleInstanceGuard();
        Assert.True(replacement.IsFirstInstance);
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

