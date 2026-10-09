namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

public class LaunchWindowViewModelDialogTests
{
    [Fact]
    public void AddProject_WhenFolderSelectionIsCancelled_UsesDialogServiceAndDoesNotMutateProjects()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());

        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences());

        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns((string?)null);

        var notifications = new Mock<IUserNotificationService>(MockBehavior.Strict);
        var lifetime = new Mock<IApplicationLifetime>(MockBehavior.Strict);
        lifetime.Setup(x => x.Shutdown());
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var projectPane = new ProjectPaneController(projectService.Object, dialogs.Object, new PromptState());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            operationLifetime.Object);
        var updateControl = CreateUpdateControl(operationLifetime.Object);
        var statusCoordinator = CreateStatusCoordinator(projectPane, agentPane, updateControl);
        var viewModel = new LaunchWindowViewModel(
            projectPane,
            agentPane,
            new LaunchCommandCoordinator(
                new Mock<IAgentCommandWorkflow>().Object,
                operationLifetime.Object,
                new Mock<IUserNotificationService>().Object,
                NullLogger<LaunchCommandCoordinator>.Instance),
            preferences.Object,
            new LaunchWindowActionBuilder(),
            updateControl,
            new Mock<ISettingsLauncher>().Object,
            notifications.Object,
            lifetime.Object,
            new Mock<IExternalLauncher>().Object,
            statusCoordinator);

        viewModel.AddProjectCommand.Execute(null);

        dialogs.Verify(x => x.SelectProjectFolder(), Times.Once);
        projectService.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
        Assert.Empty(viewModel.Projects);

        viewModel.ExitCommand.Execute(null);

        lifetime.Verify(x => x.Shutdown(), Times.Once);
    }

    private static UpdateControlViewModel CreateUpdateControl(IApplicationOperationLifetime operationLifetime)
    {
        var updates = new Mock<IUpdateWorkflow>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        return new UpdateControlViewModel(
            updates.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            operationLifetime);
    }

    private static StatusMessageCoordinator CreateStatusCoordinator(
        ProjectPaneController projectPane,
        AgentPaneController agentPane,
        UpdateControlViewModel updateControl)
    {
        return new StatusMessageCoordinator(projectPane, agentPane, updateControl);
    }

    [Fact]
    public void Commands_ArePassThroughToControllers()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());

        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences());

        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            operationLifetime.Object);
        var updateControl = CreateUpdateControl(operationLifetime.Object);
        var statusCoordinator = CreateStatusCoordinator(projectPane, agentPane, updateControl);
        var viewModel = new LaunchWindowViewModel(
            projectPane,
            agentPane,
            new LaunchCommandCoordinator(
                new Mock<IAgentCommandWorkflow>().Object,
                operationLifetime.Object,
                new Mock<IUserNotificationService>().Object,
                NullLogger<LaunchCommandCoordinator>.Instance),
            preferences.Object,
            new LaunchWindowActionBuilder(),
            updateControl,
            new Mock<ISettingsLauncher>().Object,
            new Mock<IUserNotificationService>().Object,
            new Mock<IApplicationLifetime>().Object,
            new Mock<IExternalLauncher>().Object,
            statusCoordinator);

        Assert.Same(projectPane.AddCommand, viewModel.AddProjectCommand);
        Assert.Same(projectPane.RemoveCommand, viewModel.RemoveProjectCommand);
        Assert.Same(projectPane.ToggleFavoriteCommand, viewModel.ToggleFavoriteCommand);
        Assert.Same(projectPane.RefreshCommand, viewModel.RefreshCommand);
    }

    [Fact]
    public void Dispose_RemovesEventSubscriptions()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());

        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences());

        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            operationLifetime.Object);
        var updateControl = CreateUpdateControl(operationLifetime.Object);
        var statusCoordinator = CreateStatusCoordinator(projectPane, agentPane, updateControl);
        var viewModel = new LaunchWindowViewModel(
            projectPane,
            agentPane,
            new LaunchCommandCoordinator(
                new Mock<IAgentCommandWorkflow>().Object,
                operationLifetime.Object,
                new Mock<IUserNotificationService>().Object,
                NullLogger<LaunchCommandCoordinator>.Instance),
            preferences.Object,
            new LaunchWindowActionBuilder(),
            updateControl,
            new Mock<ISettingsLauncher>().Object,
            new Mock<IUserNotificationService>().Object,
            new Mock<IApplicationLifetime>().Object,
            new Mock<IExternalLauncher>().Object,
            statusCoordinator);

        var projectsChangedCount = 0;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedProject))
            {
                projectsChangedCount++;
            }
        };

        viewModel.Dispose();

        projectService.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(0, projectsChangedCount);
    }
}
