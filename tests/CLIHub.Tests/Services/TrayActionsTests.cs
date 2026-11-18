namespace CLIHub.Tests.Services;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Tray;
using CLIHub.Updates;

using Moq;

public class TrayActionsTests
{
    [Fact]
    public void GetState_ProjectsFavoritesWithResolvedDefaultInitializedAgentsAndUpdateState()
    {
        var current = CreateProject("current");
        var favorite = CreateProject("favorite", isFavorite: true);
        var launchable = CreatePlugin("launchable", hasLaunchCommand: true);
        var nonLaunchable = CreatePlugin("other", hasLaunchCommand: false);
        var projects = new Mock<IProjectService>();
        projects.Setup(x => x.GetAllProjects()).Returns([current, favorite]);
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns([launchable, nonLaunchable]);
        var updates = new Mock<IUpdateWorkflow>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");
        updates.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0.0");
        updates.SetupGet(x => x.IsDownloading).Returns(false);
        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences { DefaultAgentId = launchable.Id });
        var actions = CreateActions(
            projects.Object,
            catalog.Object,
            updates.Object,
            preferences: preferences.Object);

        var state = actions.GetState();

        Assert.Equal("1.2.1", state.Version);
        var entry = Assert.Single(state.FavoriteProjects);
        Assert.Same(favorite, entry.Project);
        Assert.Same(launchable, entry.DefaultAgent);
        Assert.Equal([launchable], entry.InitializedAgents);
        Assert.Equal("2.0.0", state.AvailableUpdateVersion);
        Assert.False(state.IsDownloadingUpdate);
    }

    [Fact]
    public async Task SelectProjectAndLaunchAgentCommand_WhenInvoked_SelectsProjectThenLaunches()
    {
        var project = CreateProject("project");
        var plugin = CreatePlugin("agent", hasLaunchCommand: true);
        var projects = new Mock<IProjectService>(MockBehavior.Strict);
        projects.Setup(x => x.SetCurrentProject(project.Id));
        projects.Setup(x => x.GetCurrentProject()).Returns(project);
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(x => x.ExecuteAsync(plugin, project, AgentCommandKind.Launch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var actions = CreateActions(
            projects.Object,
            new Mock<IPluginCatalog>().Object,
            new Mock<IUpdateWorkflow>().Object,
            workflow.Object);

        await actions.Commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin);

        projects.Verify(x => x.SetCurrentProject(project.Id), Times.Once);
        projects.Verify(x => x.GetCurrentProject(), Times.Once);
        workflow.Verify(
            x => x.ExecuteAsync(plugin, project, AgentCommandKind.Launch, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CheckForUpdatesCommand_UsesSharedCheckerAndOperationLifetime()
    {
        var checker = new Mock<IUpdateWorkflow>();
        checker
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0.0", null));
        var operationLifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        operationLifetime
            .Setup(x => x.RunAsync("Manual update check", It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) =>
            {
                operation(CancellationToken.None);
                return Task.CompletedTask;
            });
        var actions = CreateActions(
            new Mock<IProjectService>().Object,
            new Mock<IPluginCatalog>().Object,
            checker.Object,
            operationLifetime: operationLifetime.Object);

        actions.Commands.CheckForUpdates();

        checker.Verify(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesFromStateChanges()
    {
        var projects = new Mock<IProjectService>();
        var actions = CreateActions(
            projects.Object,
            new Mock<IPluginCatalog>().Object,
            new Mock<IUpdateWorkflow>().Object);
        var changed = 0;
        actions.StateChanged += (_, _) => changed++;

        actions.Dispose();
        projects.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void CreateCommands_ControllerOwnedSlots_AreInertDefaults()
    {
        var projects = new Mock<IProjectService>(MockBehavior.Strict);
        var updateWorkflow = new Mock<IUpdateWorkflow>(MockBehavior.Strict);
        var handlers = new TrayCommandHandlers(
            projects.Object,
            new Mock<IAgentCommandWorkflow>(MockBehavior.Strict).Object,
            updateWorkflow.Object,
            new Mock<IApplicationOperationLifetime>(MockBehavior.Strict).Object,
            new Mock<IProjectDialogService>(MockBehavior.Strict).Object,
            new Mock<IUserNotificationService>(MockBehavior.Strict).Object,
            new Mock<ISettingsLauncher>(MockBehavior.Strict).Object,
            new Mock<IReleaseNotesLauncher>(MockBehavior.Strict).Object);
        var commands = handlers.CreateCommands();

        commands.RequestUpdateDownload();
        commands.Exit();

        projects.VerifyNoOtherCalls();
        updateWorkflow.VerifyNoOtherCalls();
    }

    private static TrayActions CreateActions(
        IProjectService projects,
        IPluginCatalog catalog,
        IUpdateWorkflow updates,
        IAgentCommandWorkflow? workflow = null,
        IApplicationOperationLifetime? operationLifetime = null,
        IAgentDetectionService? detection = null,
        IPreferencesStore? preferences = null)
    {
        var detectionMock = new Mock<IAgentDetectionService>();
        detectionMock.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detectionMock.Setup(x => x.IsAvailableInProject(It.IsAny<Plugin>(), It.IsAny<string>())).Returns(true);
        var preferencesMock = new Mock<IPreferencesStore>();
        preferencesMock.Setup(x => x.Load()).Returns(new AppPreferences());
        var projection = new TrayStateProjection(
            projects,
            catalog,
            updates,
            detection ?? detectionMock.Object,
            preferences ?? preferencesMock.Object);
        var handlers = new TrayCommandHandlers(
            projects,
            workflow ?? new Mock<IAgentCommandWorkflow>().Object,
            updates,
            operationLifetime ?? new Mock<IApplicationOperationLifetime>().Object,
            new Mock<IProjectDialogService>().Object,
            new Mock<IUserNotificationService>().Object,
            new Mock<ISettingsLauncher>().Object,
            new Mock<IReleaseNotesLauncher>().Object);
        return new TrayActions(projection, handlers);
    }

    private static Project CreateProject(string id, bool isFavorite = false) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}",
        IsFavorite = isFavorite
    };

    private static Plugin CreatePlugin(string id, bool hasLaunchCommand) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands
        {
            Launch = hasLaunchCommand ? new PluginCommand { Executable = id } : null
        }
    };
}
