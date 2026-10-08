namespace CLIHub.Tests;

using Moq;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

public sealed class OptionalStartupCoordinatorTests
{
    [Fact]
    public void RunOptionalStartup_ExecutesReleaseNotesAndUpdateCheck()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        var startupState = new StartupState(new AppPreferences
        {
            CheckForUpdatesOnStartup = true
        });
        var startupUi = new Mock<IApplicationStartupUi>();
        var context = new ApplicationStartupContext(_ => { }, _ => { });

        operationLifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(CancellationToken.None));

        releaseNotesStartup.Setup(x => x.Evaluate(startupState.Preferences));
        updateStartup.Setup(x => x.CheckAsync(true, It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new OptionalStartupCoordinator(
            operationLifetime.Object,
            releaseNotesStartup.Object,
            updateStartup.Object,
            NullLogger<OptionalStartupCoordinator>.Instance);

        // Act
        coordinator.RunOptionalStartup(startupState, startupUi.Object, context);

        // Assert
        releaseNotesStartup.Verify(x => x.Evaluate(startupState.Preferences), Times.Once);
        updateStartup.Verify(x => x.CheckAsync(true, It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RunOptionalStartup_ReleaseNotesFailure_ContinuesWithUpdateCheck()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        var startupState = new StartupState(new AppPreferences
        {
            CheckForUpdatesOnStartup = false
        });
        var startupUi = new Mock<IApplicationStartupUi>();
        var context = new ApplicationStartupContext(_ => { }, _ => { });

        operationLifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(CancellationToken.None));

        releaseNotesStartup.Setup(x => x.Evaluate(startupState.Preferences))
            .Throws(new InvalidOperationException("test"));
        updateStartup.Setup(x => x.CheckAsync(false, It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new OptionalStartupCoordinator(
            operationLifetime.Object,
            releaseNotesStartup.Object,
            updateStartup.Object,
            NullLogger<OptionalStartupCoordinator>.Instance);

        // Act
        coordinator.RunOptionalStartup(startupState, startupUi.Object, context);

        // Assert
        releaseNotesStartup.Verify(x => x.Evaluate(startupState.Preferences), Times.Once);
        updateStartup.Verify(x => x.CheckAsync(false, It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RunOptionalStartup_UpdateCheckFailure_DoesNotThrow()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        var startupState = new StartupState(new AppPreferences
        {
            CheckForUpdatesOnStartup = true
        });
        var startupUi = new Mock<IApplicationStartupUi>();
        var context = new ApplicationStartupContext(_ => { }, _ => { });

        operationLifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(CancellationToken.None));

        releaseNotesStartup.Setup(x => x.Evaluate(startupState.Preferences));
        updateStartup.Setup(x => x.CheckAsync(true, It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("test"));

        var coordinator = new OptionalStartupCoordinator(
            operationLifetime.Object,
            releaseNotesStartup.Object,
            updateStartup.Object,
            NullLogger<OptionalStartupCoordinator>.Instance);

        // Act & Assert (should not throw)
        coordinator.RunOptionalStartup(startupState, startupUi.Object, context);

        releaseNotesStartup.Verify(x => x.Evaluate(startupState.Preferences), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullOperationLifetime_ThrowsArgumentNullException()
    {
        // Arrange
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new OptionalStartupCoordinator(
                null!,
                releaseNotesStartup.Object,
                updateStartup.Object,
                NullLogger<OptionalStartupCoordinator>.Instance));

        Assert.Equal("operationLifetime", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullReleaseNotesStartup_ThrowsArgumentNullException()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new OptionalStartupCoordinator(
                operationLifetime.Object,
                null!,
                updateStartup.Object,
                NullLogger<OptionalStartupCoordinator>.Instance));

        Assert.Equal("releaseNotesStartup", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullUpdateStartup_ThrowsArgumentNullException()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new OptionalStartupCoordinator(
                operationLifetime.Object,
                releaseNotesStartup.Object,
                null!,
                NullLogger<OptionalStartupCoordinator>.Instance));

        Assert.Equal("updateStartup", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var releaseNotesStartup = new Mock<IReleaseNotesStartupCoordinator>();
        var updateStartup = new Mock<IUpdateStartupCoordinator>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new OptionalStartupCoordinator(
                operationLifetime.Object,
                releaseNotesStartup.Object,
                updateStartup.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }
}
