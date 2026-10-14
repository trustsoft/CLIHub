namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Windows;

public class SingleInstanceGuardTests
{
    [Fact]
    public void FirstGuard_IsFirstInstance()
    {
        var (mutexName, pipeName) = CreateUniqueNames();

        using var guard = new SingleInstanceGuard(mutexName, pipeName);

        Assert.True(guard.IsFirstInstance);
    }

    [Fact]
    public void SecondGuard_IsNotFirstInstance_WhileFirstHolds()
    {
        var (mutexName, pipeName) = CreateUniqueNames();

        using var first = new SingleInstanceGuard(mutexName, pipeName);
        using var second = new SingleInstanceGuard(mutexName, pipeName);

        Assert.True(first.IsFirstInstance);
        Assert.False(second.IsFirstInstance);
    }

    [Fact]
    public void ServiceProviderDisposal_ReleasesFirstInstanceMutex()
    {
        var (mutexName, pipeName) = CreateUniqueNames();

        var services = new ServiceCollection();
        services.AddSingleton(_ => new SingleInstanceGuard(mutexName, pipeName));

        using (var provider = services.BuildServiceProvider())
        {
            Assert.True(provider.GetRequiredService<SingleInstanceGuard>().IsFirstInstance);
        }

        using var replacement = new SingleInstanceGuard(mutexName, pipeName);
        Assert.True(replacement.IsFirstInstance);
    }

    /// <summary>
    ///   Unique per-test names keep guards isolated from parallel test processes that share the
    ///   Windows session with the production mutex name.
    /// </summary>
    private static (string MutexName, string PipeName) CreateUniqueNames()
        => ($@"Local\CLIHub.Tests.{Guid.NewGuid():N}", $"CLIHub.Tests.{Guid.NewGuid():N}");
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
