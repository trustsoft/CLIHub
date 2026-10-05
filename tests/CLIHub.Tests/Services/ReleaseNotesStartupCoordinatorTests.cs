namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

public class ReleaseNotesStartupCoordinatorTests
{
    [Fact]
    public void Evaluate_WithNewNotes_ShowsReleaseNotes()
    {
        var updateService = CreateUpdateService("1.2.0");
        var notes = new Mock<IReleaseNotesService>(MockBehavior.Strict);
        notes.Setup(x => x.GetNote("1.2.0")).Returns(new ReleaseNote("1.2.0", "2026-10-05", [], [], []));
        var launcher = CreateLauncher();
        var coordinator = CreateCoordinator(updateService.Object, notes.Object, launcher.Object);

        coordinator.Evaluate(new AppPreferences { LastSeenReleaseNotesVersion = "1.1.0" });

        launcher.Verify(x => x.ShowReleaseNotes(), Times.Once);
        launcher.Verify(x => x.MarkReleaseNotesSeen(), Times.Never);
    }

    [Fact]
    public void Evaluate_OnFirstRun_RecordsVersionWithoutShowing()
    {
        var updateService = CreateUpdateService("1.2.0");
        var notes = new Mock<IReleaseNotesService>(MockBehavior.Strict);
        notes.Setup(x => x.GetNote("1.2.0")).Returns((ReleaseNote?)null);
        var launcher = CreateLauncher();
        var coordinator = CreateCoordinator(updateService.Object, notes.Object, launcher.Object);

        coordinator.Evaluate(new AppPreferences());

        launcher.Verify(x => x.MarkReleaseNotesSeen(), Times.Once);
        launcher.Verify(x => x.ShowReleaseNotes(), Times.Never);
    }

    [Fact]
    public void Evaluate_WhenAlreadySeen_DoesNothing()
    {
        var updateService = CreateUpdateService("1.2.0");
        var notes = new Mock<IReleaseNotesService>(MockBehavior.Strict);
        notes.Setup(x => x.GetNote("1.2.0")).Returns(new ReleaseNote("1.2.0", "2026-10-05", [], [], []));
        var launcher = CreateLauncher();
        var coordinator = CreateCoordinator(updateService.Object, notes.Object, launcher.Object);

        coordinator.Evaluate(new AppPreferences { LastSeenReleaseNotesVersion = "1.2.0" });

        launcher.Verify(x => x.ShowReleaseNotes(), Times.Never);
        launcher.Verify(x => x.MarkReleaseNotesSeen(), Times.Never);
    }

    [Fact]
    public void Evaluate_WhenDependencyFails_DoesNotThrow()
    {
        var updateService = new Mock<IUpdateService>(MockBehavior.Strict);
        updateService.Setup(x => x.GetCurrentVersion()).Throws(new InvalidOperationException("test"));
        var coordinator = CreateCoordinator(updateService.Object, Mock.Of<IReleaseNotesService>(), CreateLauncher().Object);

        var exception = Record.Exception(() => coordinator.Evaluate(new AppPreferences()));

        Assert.Null(exception);
    }

    [Fact]
    public void AddClIHubServices_RegistersReleaseNotesStartupCoordinator()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReleaseNotesStartupCoordinator>(
            provider.GetRequiredService<IReleaseNotesStartupCoordinator>());
    }

    private static Mock<IUpdateService> CreateUpdateService(string version)
    {
        var service = new Mock<IUpdateService>(MockBehavior.Strict);
        service.Setup(x => x.GetCurrentVersion()).Returns(version);
        return service;
    }

    private static Mock<IReleaseNotesLauncher> CreateLauncher() =>
        new(MockBehavior.Strict);

    private static ReleaseNotesStartupCoordinator CreateCoordinator(
        IUpdateService updateService,
        IReleaseNotesService releaseNotes,
        IReleaseNotesLauncher launcher) =>
        new(
            updateService,
            releaseNotes,
            launcher,
            NullLogger<ReleaseNotesStartupCoordinator>.Instance);
}
