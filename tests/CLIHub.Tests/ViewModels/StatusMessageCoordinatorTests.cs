namespace CLIHub.Tests.ViewModels;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

public class StatusMessageCoordinatorTests
{
    [Fact]
    public void Constructor_SetsInitialMessage()
    {
        var coordinator = CreateCoordinator();

        Assert.Equal("CLIHub ready", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportProjectSelected_SetsCorrectMessageFormat()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportProjectSelected("MyProject");

        Assert.Equal("Current project: MyProject", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportAgentSelected_SetsCorrectMessageFormat()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportAgentSelected("MyAgent");

        Assert.Equal("Selected agent: MyAgent", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportWindowPinChanged_WhenPinned_SetsPinnedMessage()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportWindowPinChanged(true);

        Assert.Equal("Window pinned open", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportWindowPinChanged_WhenUnpinned_SetsUnpinnedMessage()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportWindowPinChanged(false);

        Assert.Equal("Window unpinned", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportCommandOutcome_ForwardsMessageExactly()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportCommandOutcome("Custom command result");

        Assert.Equal("Custom command result", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportFolderOpen_WithSuccess_SetsSuccessMessage()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportFolderOpen("C:\\TestFolder");

        Assert.Equal("Opened C:\\TestFolder", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportFolderOpen_WithError_IncludesExceptionMessage()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportFolderOpen("C:\\TestFolder", new InvalidOperationException("Access denied"));

        Assert.Equal("Could not open C:\\TestFolder: Access denied", coordinator.CurrentMessage);
    }

    [Fact]
    public void ReportNoAgentsFound_SetsCorrectMessage()
    {
        var coordinator = CreateCoordinator();

        coordinator.ReportNoAgentsFound();

        Assert.Equal("No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\", coordinator.CurrentMessage);
    }

    [Fact]
    public void ProjectsChanged_UpdatesMessageToProjectsRefreshed()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            new Mock<IApplicationOperationLifetime>().Object);
        var updateControl = CreateUpdateControl();
        var coordinator = new StatusMessageCoordinator(projectPane, agentPane, updateControl);

        projectService.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal("Projects refreshed", coordinator.CurrentMessage);
    }

    [Fact]
    public void UpdateOutcomeReported_UpdatesMessageFromEvent()
    {
        var coordinator = CreateCoordinator();
        var updateControl = coordinator.GetType()
            .GetField("_updateControl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(coordinator) as UpdateControlViewModel;

        Assert.NotNull(updateControl);
        RaiseOutcomeReported(updateControl, "Update completed successfully");

        Assert.Equal("Update completed successfully", coordinator.CurrentMessage);
    }

    [Fact]
    public void Dispose_UnsubscribesFromProjectsChanged()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            new Mock<IApplicationOperationLifetime>().Object);
        var updateControl = CreateUpdateControl();
        var coordinator = new StatusMessageCoordinator(projectPane, agentPane, updateControl);

        coordinator.ReportProjectSelected("BeforeDispose");
        coordinator.Dispose();
        projectService.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal("Current project: BeforeDispose", coordinator.CurrentMessage);
    }

    [Fact]
    public void Dispose_UnsubscribesFromOutcomeReported()
    {
        var coordinator = CreateCoordinator();
        var updateControl = coordinator.GetType()
            .GetField("_updateControl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(coordinator) as UpdateControlViewModel;

        Assert.NotNull(updateControl);
        coordinator.ReportProjectSelected("BeforeDispose");
        coordinator.Dispose();
        RaiseOutcomeReported(updateControl, "Should not update");

        Assert.Equal("Current project: BeforeDispose", coordinator.CurrentMessage);
    }

    [Fact]
    public void CurrentMessage_RaisesPropertyChanged()
    {
        var coordinator = CreateCoordinator();
        var propertyChanged = false;
        coordinator.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StatusMessageCoordinator.CurrentMessage))
            {
                propertyChanged = true;
            }
        };

        coordinator.ReportProjectSelected("Test");

        Assert.True(propertyChanged);
    }

    private static StatusMessageCoordinator CreateCoordinator()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            new Mock<IApplicationOperationLifetime>().Object);
        var updateControl = CreateUpdateControl();
        return new StatusMessageCoordinator(projectPane, agentPane, updateControl);
    }

    private static UpdateControlViewModel CreateUpdateControl()
    {
        var updates = new Mock<IUpdateWorkflow>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        return new UpdateControlViewModel(
            updates.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateControlViewModel>.Instance,
            operationLifetime.Object);
    }

    private static void RaiseOutcomeReported(UpdateControlViewModel updateControl, string message)
    {
        var eventInfo = typeof(UpdateControlViewModel).GetEvent("OutcomeReported");
        var eventField = typeof(UpdateControlViewModel)
            .GetField("OutcomeReported", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var eventDelegate = eventField?.GetValue(updateControl) as MulticastDelegate;
        
        if (eventDelegate != null)
        {
            foreach (var handler in eventDelegate.GetInvocationList())
            {
                handler.Method.Invoke(handler.Target, new object[] { updateControl, message });
            }
        }
    }
}
