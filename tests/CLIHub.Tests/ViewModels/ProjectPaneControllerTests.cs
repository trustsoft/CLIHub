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

    private static Project Project(string id) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}"
    };
}
