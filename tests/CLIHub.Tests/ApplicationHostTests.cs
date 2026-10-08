namespace CLIHub.Tests;

using CLIHub;

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

    // Note: Full integration tests for Start() and Shutdown() with real WPF components
    // require STA threading and are covered by manual testing and the existing
    // ApplicationBootstrapper integration tests.
}
