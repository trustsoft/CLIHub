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

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());

        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(new AppPreferences());

        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns((string?)null);

        var notifications = new Mock<IUserNotificationService>(MockBehavior.Strict);
        var lifetime = new Mock<IApplicationLifetime>(MockBehavior.Strict);
        lifetime.Setup(x => x.Shutdown());
        var viewModel = new LaunchWindowViewModel(
            projectService.Object,
            pluginManager.Object,
            new Mock<IAgentCommandWorkflow>().Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            preferences.Object,
            CreateUpdateService(),
            new Mock<ISettingsLauncher>().Object,
            new PromptState(),
            dialogs.Object,
            notifications.Object,
            lifetime.Object,
            NullLogger<LaunchWindowViewModel>.Instance,
            NullLogger<UpdateControlViewModel>.Instance);

        viewModel.AddProjectCommand.Execute(null);

        dialogs.Verify(x => x.SelectProjectFolder(), Times.Once);
        projectService.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
        Assert.Empty(viewModel.Projects);

        viewModel.ExitCommand.Execute(null);

        lifetime.Verify(x => x.Shutdown(), Times.Once);
    }

    private static IUpdateService CreateUpdateService()
    {
        var updates = new Mock<IUpdateService>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        return updates.Object;
    }
}
