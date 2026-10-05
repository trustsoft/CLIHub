namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

public class UpdateStartupCoordinatorTests
{
    [Fact]
    public async Task CheckAsync_Disabled_DoesNotCallUpdateService()
    {
        var updateService = new Mock<IUpdateService>(MockBehavior.Strict);
        var coordinator = CreateCoordinator(updateService.Object);

        await coordinator.CheckAsync(false, _ => Assert.Fail("Notification was not expected."));

        updateService.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAsync_AvailableUpdate_NotifiesVersion()
    {
        var updateService = CreateUpdateService(
            new UpdateCheckResult(UpdateStatus.UpdateAvailable, "1.0.0", "1.1.0"));
        var coordinator = CreateCoordinator(updateService.Object);
        string? notifiedVersion = null;

        await coordinator.CheckAsync(true, version => notifiedVersion = version);

        Assert.Equal("1.1.0", notifiedVersion);
    }

    [Theory]
    [InlineData(UpdateStatus.UpToDate)]
    [InlineData(UpdateStatus.NotInstalled)]
    [InlineData(UpdateStatus.Failed)]
    public async Task CheckAsync_NonAvailableResult_DoesNotNotify(UpdateStatus status)
    {
        var updateService = CreateUpdateService(new UpdateCheckResult(status, "1.0.0", null));
        var coordinator = CreateCoordinator(updateService.Object);
        var notificationCount = 0;

        await coordinator.CheckAsync(true, _ => notificationCount++);

        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public async Task CheckAsync_WhenServiceThrows_DoesNotThrow()
    {
        var updateService = new Mock<IUpdateService>(MockBehavior.Strict);
        updateService
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test"));
        var coordinator = CreateCoordinator(updateService.Object);

        var exception = await Record.ExceptionAsync(() => coordinator.CheckAsync(true, _ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void AddClIHubServices_RegistersUpdateStartupCoordinator()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<UpdateStartupCoordinator>(provider.GetRequiredService<IUpdateStartupCoordinator>());
    }

    private static Mock<IUpdateService> CreateUpdateService(UpdateCheckResult result)
    {
        var service = new Mock<IUpdateService>(MockBehavior.Strict);
        service
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return service;
    }

    private static UpdateStartupCoordinator CreateCoordinator(IUpdateService updateService) =>
        new(updateService, NullLogger<UpdateStartupCoordinator>.Instance);
}
