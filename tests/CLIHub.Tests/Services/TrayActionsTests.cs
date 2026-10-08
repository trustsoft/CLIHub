namespace CLIHub.Tests.Services;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;

public class TrayActionsTests
{
    [Fact]
    public void GetState_ProjectsCurrentRecentLaunchableAndUpdateState()
    {
        var current = CreateProject("current");
        var recent = CreateProject("recent");
        var launchable = CreatePlugin("launchable", hasLaunchCommand: true);
        var nonLaunchable = CreatePlugin("other", hasLaunchCommand: false);
        var projects = new Mock<IProjectService>();
        projects.Setup(x => x.GetCurrentProject()).Returns(current);
        projects.Setup(x => x.GetRecentProjects(10)).Returns([recent]);
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns([launchable, nonLaunchable]);
        var updates = new Mock<IUpdateService>();
        updates.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0.0");
        updates.SetupGet(x => x.IsDownloading).Returns(false);
        var actions = CreateActions(projects.Object, catalog.Object, updates.Object);

        var state = actions.GetState();

        Assert.Same(current, state.CurrentProject);
        Assert.Equal([recent], state.RecentProjects);
        Assert.Equal([launchable], state.LaunchableAgents);
        Assert.Equal("2.0.0", state.AvailableUpdateVersion);
        Assert.False(state.IsDownloadingUpdate);
    }

    [Fact]
    public async Task LaunchAgentCommand_UsesSharedAgentWorkflow()
    {
        var project = CreateProject("project");
        var plugin = CreatePlugin("agent", hasLaunchCommand: true);
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(x => x.ExecuteAsync(plugin, project, AgentCommandKind.Launch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var actions = CreateActions(
            new Mock<IProjectService>().Object,
            new Mock<IPluginCatalog>().Object,
            new Mock<IUpdateService>().Object,
            workflow.Object);

        await actions.Commands.LaunchAgentAsync(plugin, project);

        workflow.Verify(
            x => x.ExecuteAsync(plugin, project, AgentCommandKind.Launch, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CheckForUpdatesCommand_UsesSharedCheckerAndOperationLifetime()
    {
        var checker = new Mock<IUpdateChecker>(MockBehavior.Strict);
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
            new Mock<IUpdateService>().Object,
            updateChecker: checker.Object,
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
            new Mock<IUpdateService>().Object);
        var changed = 0;
        actions.StateChanged += (_, _) => changed++;

        actions.Dispose();
        projects.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(0, changed);
    }

    private static TrayActions CreateActions(
        IProjectService projects,
        IPluginCatalog catalog,
        IUpdateService updates,
        IAgentCommandWorkflow? workflow = null,
        IUpdateChecker? updateChecker = null,
        IApplicationOperationLifetime? operationLifetime = null)
    {
        var projection = new TrayStateProjection(projects, catalog, updates);
        var handlers = new TrayCommandHandlers(
            projects,
            workflow ?? new Mock<IAgentCommandWorkflow>().Object,
            updates,
            updateChecker ?? new Mock<IUpdateChecker>().Object,
            operationLifetime ?? new Mock<IApplicationOperationLifetime>().Object,
            new Mock<IProjectDialogService>().Object,
            new Mock<IUserNotificationService>().Object,
            new Mock<ISettingsLauncher>().Object,
            new Mock<IReleaseNotesLauncher>().Object,
            new Mock<IApplicationLifetime>().Object,
            projection);
        return new TrayActions(projection, handlers);
    }

    private static Project CreateProject(string id) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}"
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
