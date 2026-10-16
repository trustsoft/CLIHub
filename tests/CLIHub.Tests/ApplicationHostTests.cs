namespace CLIHub.Tests;

using System.Threading.Tasks;

using CLIHub.Core.Windows;
using CLIHub.Startup;

public sealed class ApplicationHostTests
{
    [Fact]
    public void Shutdown_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var host = new ApplicationHost();

        // Act & Assert (should not throw)
        host.Shutdown();
    }

    [Fact]
    public void Start_WithNullDispatch_ThrowsArgumentNullException()
    {
        // Arrange
        var host = new ApplicationHost();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            host.Start(null!, window => { }));

        Assert.Equal("dispatch", ex.ParamName);
    }

    [Fact]
    public void Start_WithNullSetMainWindow_ThrowsArgumentNullException()
    {
        // Arrange
        var host = new ApplicationHost();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            host.Start(action => action(), null!));

        Assert.Equal("setMainWindow", ex.ParamName);
    }

    [Fact]
    public async Task Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup()
    {
        // Arrange: a running first instance owns the mutex and pipe; the host's guard becomes a duplicate.
        var mutexName = $@"Local\CLIHub.Tests.{Guid.NewGuid():N}";
        var pipeName = $"CLIHub.Tests.{Guid.NewGuid():N}";

        using var first = new SingleInstanceGuard(mutexName, pipeName);
        Assert.True(first.IsFirstInstance);

        var activationObserved = new TaskCompletionSource();
        first.ActivationRequested += () => activationObserved.TrySetResult();

        var createdGuards = new List<SingleInstanceGuard>();
        var host = new ApplicationHost(() =>
        {
            var guard = new SingleInstanceGuard(mutexName, pipeName);
            createdGuards.Add(guard);
            return guard;
        });

        // Act & Assert: the duplicate signals the first instance and returns before environment setup.
        host.Start(action => action(), window => { });

        await activationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(host.HasBuiltServiceProvider);

        host.Shutdown();
        Assert.All(createdGuards, guard => Assert.True(guard.IsDisposed));
    }

    [Fact]
    public void Start_WithNullSetMainWindow_DisposesGuardAcquiredBeforeTheFailure()
    {
        // Arrange
        var createdGuards = new List<SingleInstanceGuard>();
        var host = new ApplicationHost(() =>
        {
            var guard = new SingleInstanceGuard(
                $@"Local\CLIHub.Tests.{Guid.NewGuid():N}",
                $"CLIHub.Tests.{Guid.NewGuid():N}");
            createdGuards.Add(guard);
            return guard;
        });

        // Act & Assert: the pre-provider failure releases the guard this host acquired.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            host.Start(action => action(), null!));

        Assert.Equal("setMainWindow", ex.ParamName);
        Assert.False(host.HasBuiltServiceProvider);
        Assert.All(createdGuards, guard => Assert.True(guard.IsDisposed));
    }

    // Note: Full integration tests for Start() and Shutdown() with real WPF components
    // require STA threading and are covered by manual testing and the existing
    // ApplicationBootstrapper integration tests.
}
