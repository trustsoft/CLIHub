namespace CLIHub.Tests;

using Moq;

using CLIHub;
using CLIHub.Core.Infrastructure.Windows;

public sealed class InstanceCoordinatorTests
{
    [Fact]
    public void Coordinate_WhenFirstInstance_ReturnsFirstInstance()
    {
        // Arrange
        var singleInstanceGuard = new Mock<ISingleInstanceGuard>();
        singleInstanceGuard.Setup(x => x.IsFirstInstance).Returns(true);

        var applicationLifetime = new Mock<IApplicationLifetime>();

        var coordinator = new InstanceCoordinator(
            singleInstanceGuard.Object,
            applicationLifetime.Object);

        // Act
        var result = coordinator.Coordinate();

        // Assert
        Assert.Equal(InstanceStatus.FirstInstance, result);
        singleInstanceGuard.Verify(x => x.SignalActivation(), Times.Never);
        applicationLifetime.Verify(x => x.Shutdown(), Times.Never);
    }

    [Fact]
    public void Coordinate_WhenSecondInstance_SignalsActivationAndShutdown()
    {
        // Arrange
        var singleInstanceGuard = new Mock<ISingleInstanceGuard>();
        singleInstanceGuard.Setup(x => x.IsFirstInstance).Returns(false);

        var applicationLifetime = new Mock<IApplicationLifetime>();

        var coordinator = new InstanceCoordinator(
            singleInstanceGuard.Object,
            applicationLifetime.Object);

        // Act
        var result = coordinator.Coordinate();

        // Assert
        Assert.Equal(InstanceStatus.SecondInstance, result);
        singleInstanceGuard.Verify(x => x.SignalActivation(), Times.Once);
        applicationLifetime.Verify(x => x.Shutdown(), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullSingleInstanceGuard_ThrowsArgumentNullException()
    {
        // Arrange
        var applicationLifetime = new Mock<IApplicationLifetime>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new InstanceCoordinator(null!, applicationLifetime.Object));

        Assert.Equal("singleInstanceGuard", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullApplicationLifetime_ThrowsArgumentNullException()
    {
        // Arrange
        var singleInstanceGuard = new Mock<ISingleInstanceGuard>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new InstanceCoordinator(singleInstanceGuard.Object, null!));

        Assert.Equal("applicationLifetime", ex.ParamName);
    }
}
