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
using Xunit;

/// <summary>
///   Tests for <see cref="WindowActionCoordinator"/>.
/// </summary>
public sealed class WindowActionCoordinatorTests
{
    [Fact]
    public void Constructor_CreatesAllCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.WindowActions);
        Assert.NotNull(viewModel.WindowActions.LaunchCommand);
        Assert.NotNull(viewModel.WindowActions.ResumeCommand);
        Assert.NotNull(viewModel.WindowActions.InitCommand);
        Assert.NotNull(viewModel.WindowActions.UpdateCommand);
        Assert.NotNull(viewModel.WindowActions.VersionCommand);
        Assert.NotNull(viewModel.WindowActions.OpenSettingsCommand);
        Assert.NotNull(viewModel.WindowActions.OpenDataFolderCommand);
        Assert.NotNull(viewModel.WindowActions.ExitCommand);
    }

    [Fact]
    public void LaunchCommand_CannotExecute_WhenNoAgentSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.WindowActions.LaunchCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void LaunchCommand_CanExecute_WhenAgentSelected()
    {
        // Arrange
        var plugin = CreatePluginWithCommand(AgentCommandKind.Launch);
        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(new[] { plugin });

        var viewModel = CreateViewModel(pluginCatalog: pluginCatalog.Object);

        var agent = new AgentItem
        {
            Plugin = plugin,
            Name = "Test Agent",
            IsAvailable = true
        };

        viewModel.SelectedAgent = agent;

        // Act
        var canExecute = viewModel.WindowActions.LaunchCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ResumeCommand_CannotExecute_WhenNoAgentSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.WindowActions.ResumeCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void OpenDataFolderCommand_CanAlwaysExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.WindowActions.OpenDataFolderCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void OpenSettingsCommand_CanAlwaysExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.WindowActions.OpenSettingsCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ExitCommand_CanAlwaysExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.WindowActions.ExitCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    private static LaunchWindowViewModel CreateViewModel(IPluginCatalog? pluginCatalog = null)
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

        var catalog = pluginCatalog ?? CreateMockPluginCatalog();
        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences());

        var operationLifetime = new Mock<IApplicationOperationLifetime>();
        operationLifetime.Setup(x => x.Token).Returns(CancellationToken.None);

        var projectPane = new ProjectPaneController(
            projectService.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detection.Setup(x => x.IsAvailableInProject(It.IsAny<Plugin>(), It.IsAny<string>())).Returns(true);

        var agentPane = new AgentPaneController(
            catalog,
            detection.Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            operationLifetime.Object);

        var updateControl = CreateUpdateControl(operationLifetime.Object);
        var statusCoordinator = new StatusMessageCoordinator(projectPane, agentPane, updateControl);

        return new LaunchWindowViewModel(
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

    private static Plugin CreatePluginWithCommand(AgentCommandKind kind)
    {
        var commands = new AgentCommands();
        var command = new PluginCommand { Executable = "test" };

        switch (kind)
        {
            case AgentCommandKind.Launch:
                commands.Launch = command;
                break;
            case AgentCommandKind.Resume:
                commands.Resume = command;
                break;
            case AgentCommandKind.Init:
                commands.Init = command;
                break;
            case AgentCommandKind.Update:
                commands.Update = command;
                break;
            case AgentCommandKind.Version:
                commands.Version = command;
                break;
        }

        return new Plugin
        {
            Id = "test-agent",
            Name = "Test Agent",
            Commands = commands
        };
    }

    private static IPluginCatalog CreateMockPluginCatalog()
    {
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());
        return catalog.Object;
    }
}
