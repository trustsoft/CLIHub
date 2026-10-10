namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.Tests.Services;
using CLIHub.ViewModels;

using Moq;

public class SharedUpdatePresentationTests
{
    [Theory]
    [InlineData(UpdateStatus.UpdateAvailable)]
    [InlineData(UpdateStatus.UpToDate)]
    [InlineData(UpdateStatus.NotInstalled)]
    [InlineData(UpdateStatus.Failed)]
    public async Task StartupCheck_AllSurfaces_ShowSharedProgressAndResult(UpdateStatus status)
    {
        var pending = new TaskCompletionSource<UpdateCheckResult>();
        var core = new Mock<IUpdateService>();
        core.Setup(x => x.GetCurrentVersion()).Returns("1.0");
        core.Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>())).Returns(pending.Task);
        using var workflow = UpdateWorkflowTests.Create(core.Object);
        using var projection = CreateProjection(workflow);
        var lifetime = Mock.Of<IApplicationOperationLifetime>();
        using var notes = new WhatsNewViewModel(Notes(), workflow, lifetime);
        using var control = new UpdateControlViewModel(workflow, NullLogger<UpdateControlViewModel>.Instance, lifetime);
        var startup = new UpdateStartupCoordinator(workflow, NullLogger<UpdateStartupCoordinator>.Instance);
        var settings = CreateSettingsViewModel(workflow);
        string? notified = null;

        var startupTask = startup.CheckAsync(true, version => notified = version);
        Assert.True(projection.GetState().IsCheckingForUpdates);
        Assert.Equal("Checking for updates...", notes.CheckActionText);
        Assert.False(notes.CanCheckForUpdates);
        Assert.Equal("Checking…", control.UpdateButtonText);
        Assert.False(control.UpdateControlCommand.CanExecute(null));

        var joiningCheck = workflow.CheckForUpdatesAsync();
        var settingsTask = settings.CheckForUpdatesAsync();
        var available = status == UpdateStatus.UpdateAvailable ? "2.0" : null;
        core.SetupGet(x => x.LastKnownAvailableVersion).Returns(available);
        pending.SetResult(new(status, "1.0", available));
        await Task.WhenAll(startupTask, joiningCheck, settingsTask);

        Assert.False(projection.GetState().IsCheckingForUpdates);
        Assert.Equal(available, projection.GetState().AvailableUpdateVersion);
        Assert.Equal(available, notified);
        Assert.Equal(
            status == UpdateStatus.UpdateAvailable
                ? "Update available: 2.0 (current v1.0)"
                : status == UpdateStatus.UpToDate
                    ? "Up to date (v1.0)"
                    : status == UpdateStatus.NotInstalled
                        ? "Updates apply to installed builds only."
                        : "Update check failed or timed out.",
            settings.UpdateMessage);
        Assert.Equal(available is not null, notes.CanDownloadUpdate);
        Assert.Equal(available is null, notes.CanCheckForUpdates);
        Assert.Equal(available is null ? "1.0" : "Update to 2.0", control.UpdateButtonText);
        Assert.True(control.UpdateControlCommand.CanExecute(null));
        core.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchWindowDownload_Success_RequiresExplicitRestart()
    {
        var core = new Mock<IUpdateService>();
        core.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0");
        core.Setup(x => x.GetCurrentVersion()).Returns("1.0");
        core.Setup(x => x.DownloadUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateDownloadResult(UpdateDownloadStatus.Downloaded, "2.0"));
        using var workflow = UpdateWorkflowTests.Create(core.Object);
        var operations = new List<Task>();
        var lifetime = new Mock<IApplicationOperationLifetime>();
        lifetime.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) =>
            {
                var task = operation(CancellationToken.None);
                operations.Add(task);
                return task;
            });
        using var control = new UpdateControlViewModel(workflow, NullLogger<UpdateControlViewModel>.Instance, lifetime.Object);
        control.UpdateControlCommand.Execute(null);
        await operations[^1];

        Assert.Equal("Restart to update to 2.0", control.UpdateButtonText);
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Never);
        control.UpdateControlCommand.Execute(null);
        await operations[^1];
        core.Verify(x => x.ApplyDownloadedUpdateAndRestart(), Times.Once);
    }

    private static IReleaseNotesService Notes()
    {
        var notes = new Mock<IReleaseNotesService>();
        notes.Setup(x => x.GetNotes()).Returns([]);
        return notes.Object;
    }

    private static SettingsViewModel CreateSettingsViewModel(IUpdateWorkflow workflow)
    {
        var preferences = new Mock<IPreferencesStore>();
        var startup = new Mock<IStartupService>();
        var applier = new Mock<IPreferenceApplier>();
        var application = new SettingsApplicationService(
            preferences.Object,
            startup.Object,
            applier.Object,
            NullLogger<SettingsApplicationService>.Instance);

        return new SettingsViewModel(
            application,
            workflow,
            workflow,
            Mock.Of<IApplicationOperationLifetime>(),
            NullLogger<SettingsViewModel>.Instance);
    }

    private static TrayStateProjection CreateProjection(IUpdateWorkflow workflow)
    {
        var projects = new Mock<IProjectService>();
        projects.Setup(x => x.GetRecentProjects(10)).Returns([]);
        var plugins = new Mock<IPluginCatalog>();
        plugins.Setup(x => x.GetAllPlugins()).Returns([]);
        return new(projects.Object, plugins.Object, workflow);
    }
}
