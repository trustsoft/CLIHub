namespace CLIHub.Tests.ViewModels;

using Moq;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.ViewModels;

public class ProjectPaneControllerTests
{
    [Fact]
    public void Refresh_PreservesExistingProjectIdentityAndCurrentSelection()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetAllProjects()).Returns([project]);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        var firstSelection = controller.Refresh();
        var secondSelection = controller.Refresh();

        Assert.Same(project, controller.Projects.Single());
        Assert.Same(project, firstSelection);
        Assert.Same(project, secondSelection);
    }

    [Fact]
    public void AddProject_AddsAndSelectsDialogFolder()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.AddProject("C:\\Project")).Returns(project);
        service.Setup(x => x.SetCurrentProject("project"));
        service.Setup(x => x.GetAllProjects()).Returns([project]);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns("C:\\Project");
        var controller = new ProjectPaneController(service.Object, dialogs.Object, new PromptState());

        var added = controller.AddProject();

        Assert.Same(project, added);
        service.Verify(x => x.SetCurrentProject("project"), Times.Once);
        Assert.Same(project, controller.CurrentProject);
    }

    [Fact]
    public void RemoveSelectedProject_WhenConfirmed_RemovesCurrentProject()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        service.Setup(x => x.RemoveProject("project"));
        service.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("project")).Returns(true);
        var controller = new ProjectPaneController(service.Object, dialogs.Object, new PromptState());

        Assert.True(controller.RemoveSelectedProject());
        service.Verify(x => x.RemoveProject("project"), Times.Once);
        Assert.Empty(controller.Projects);
    }

    [Fact]
    public void Dispose_UnsubscribesFromProjectChanges()
    {
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        service.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var changed = 0;
        controller.ProjectsChanged += (_, _) => changed++;

        controller.Dispose();
        service.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void AddCommand_ExecutesAddProjectWorkflow()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.AddProject("C:\\Project")).Returns(project);
        service.Setup(x => x.SetCurrentProject("project"));
        service.Setup(x => x.GetAllProjects()).Returns([project]);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns("C:\\Project");
        var controller = new ProjectPaneController(service.Object, dialogs.Object, new PromptState());

        controller.AddCommand.Execute(null);

        Assert.Single(controller.Projects);
        Assert.Same(project, controller.CurrentProject);
    }

    [Fact]
    public void RemoveCommand_CanExecuteOnlyWhenProjectSelected()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns((Project?)null)
            .Returns(project);
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        Assert.False(controller.RemoveCommand.CanExecute(null));
        Assert.True(controller.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public void RemoveCommand_ExecutesRemoveSelectedProjectWorkflow()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        service.Setup(x => x.RemoveProject("project"));
        service.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("project")).Returns(true);
        var controller = new ProjectPaneController(service.Object, dialogs.Object, new PromptState());

        controller.RemoveCommand.Execute(null);

        Assert.Empty(controller.Projects);
    }

    [Fact]
    public void ToggleFavoriteCommand_CanExecuteOnlyWhenProjectSelected()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns((Project?)null)
            .Returns(project);
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        Assert.False(controller.ToggleFavoriteCommand.CanExecute(null));
        Assert.True(controller.ToggleFavoriteCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleFavoriteCommand_ExecutesToggleFavoriteWorkflow()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        service.Setup(x => x.ToggleFavorite("project"));
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        controller.ToggleFavoriteCommand.Execute(null);

        service.Verify(x => x.ToggleFavorite("project"), Times.Once);
    }

    [Fact]
    public void RefreshCommand_ExecutesRefreshWorkflow()
    {
        var project = Project("project");
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetAllProjects()).Returns([project]);
        service.Setup(x => x.GetCurrentProject()).Returns(project);
        var controller = new ProjectPaneController(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            new PromptState());

        controller.RefreshCommand.Execute(null);

        Assert.Same(project, controller.Projects.Single());
    }

    private static Project Project(string id) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}"
    };
}
