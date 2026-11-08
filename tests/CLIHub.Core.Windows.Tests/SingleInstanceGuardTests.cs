namespace CLIHub.Core.Windows.Tests;

using System.Diagnostics;
using System.IO.Pipes;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Windows;

using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task SignalActivation_ImmediatelyAfterFirstGuardConstruction_RaisesActivationRequested()
    {
        // Regression for the v1.0.5 Release timeout: the activation pipe instance exists before the
        // first guard's constructor returns, so a duplicate that signals without any test-side delay
        // or retry is still served. On the pre-fix ordering the pipe only appeared once the
        // background server task ran, so the first client attempt could miss it entirely. Fresh
        // names per iteration repeat that window ten times.
        for (var iteration = 0; iteration < 10; iteration++)
        {
            // Arrange
            var (mutexName, pipeName) = CreateUniqueNames();

            using var first = new SingleInstanceGuard(mutexName, pipeName);
            Assert.True(first.IsFirstInstance);

            var activationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            first.ActivationRequested += () => activationObserved.TrySetResult();

            using var duplicate = new SingleInstanceGuard(mutexName, pipeName);
            Assert.False(duplicate.IsFirstInstance);

            // Act
            duplicate.SignalActivation();

            // Assert
            var observed = await Task.WhenAny(activationObserved.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            Assert.True(
                ReferenceEquals(observed, activationObserved.Task),
                $"Iteration {iteration}: the activation signal was lost.");
        }
    }

    [Fact]
    public async Task ClientConnection_OpenedImmediatelyAfterFirstGuardConstruction_IsServed()
    {
        // A client can connect as soon as the pipe instance exists, which is before the server loop
        // reaches WaitForConnectionAsync. The pending connection must still be served, using one
        // client attempt without any retry.
        for (var iteration = 0; iteration < 10; iteration++)
        {
            // Arrange
            var (mutexName, pipeName) = CreateUniqueNames();

            using var first = new SingleInstanceGuard(mutexName, pipeName);

            var activationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            first.ActivationRequested += () => activationObserved.TrySetResult();

            // Act
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                client.Connect(3000);

                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("SHOW");
            }

            // Assert
            var observed = await Task.WhenAny(activationObserved.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            Assert.True(
                ReferenceEquals(observed, activationObserved.Task),
                $"Iteration {iteration}: the pending client connection was not served.");
        }
    }

    [Fact]
    public async Task SignalActivation_TwiceOverOneGuard_DeliversBothSignals()
    {
        // Arrange: the server must recreate its pipe after each activation, so a later launch is
        // served without restarting the first instance.
        var (mutexName, pipeName) = CreateUniqueNames();

        using var first = new SingleInstanceGuard(mutexName, pipeName);
        using var duplicate = new SingleInstanceGuard(mutexName, pipeName);

        var activationCount = 0;
        var firstActivation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondActivation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.ActivationRequested += () =>
        {
            if (Interlocked.Increment(ref activationCount) == 1)
            {
                firstActivation.TrySetResult();
            }
            else
            {
                secondActivation.TrySetResult();
            }
        };

        // Act
        duplicate.SignalActivation();
        var firstObserved = await Task.WhenAny(firstActivation.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        duplicate.SignalActivation();
        var secondObserved = await Task.WhenAny(secondActivation.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        Assert.True(ReferenceEquals(firstObserved, firstActivation.Task), "The first activation signal was lost.");
        Assert.True(ReferenceEquals(secondObserved, secondActivation.Task), "The second activation signal was lost.");
    }

    [Fact]
    public void SignalActivation_ServerNeverReady_FailsSilentlyWithinBoundedDeadline()
    {
        // Arrange: the duplicate shares the first instance's mutex but targets a pipe name that never
        // had a server, so every connection attempt fails and the retry budget is fully consumed.
        var (mutexName, pipeName) = CreateUniqueNames();
        var neverServedPipeName = $"CLIHub.Tests.{Guid.NewGuid():N}";

        using var first = new SingleInstanceGuard(mutexName, pipeName);
        using var duplicate = new SingleInstanceGuard(mutexName, neverServedPipeName);
        Assert.False(duplicate.IsFirstInstance);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var exception = Record.Exception(duplicate.SignalActivation);
        stopwatch.Stop();

        // Assert
        Assert.Null(exception);
        Assert.True(
            stopwatch.Elapsed >= TimeSpan.FromSeconds(1.5),
            $"The retry deadline gave up too early ({stopwatch.Elapsed.TotalSeconds:F2} s).");
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2.5),
            $"The retry deadline overshot its budget ({stopwatch.Elapsed.TotalSeconds:F2} s).");
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
