namespace CLIHub.ViewModels;

using Moq;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

public class ProjectPaneControllerTests
{
    [Fact]
    public void Refresh_PreservesRowIdentityAndRestoresSelectionById()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service);

        var firstSelection = controller.Refresh();
        var secondSelection = controller.Refresh();

        Assert.Single(controller.Projects);
        Assert.Same(firstSelection, controller.Projects.Single());
        Assert.Same(firstSelection, secondSelection);
        Assert.Equal("project", firstSelection?.Id);
    }

    [Fact]
    public void Refresh_ServiceStateChanged_UpdatesRowInPlace()
    {
        var project = Project("project");
        var favorite = Project("project");
        favorite.IsFavorite = true;
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service);
        controller.Refresh();

        service.Setup(x => x.GetAllProjects()).Returns([favorite]);
        var selection = controller.Refresh();

        Assert.Same(controller.Projects.Single(), selection);
        Assert.True(controller.Projects.Single().IsFavorite);
    }

    [Fact]
    public void Refresh_ProjectVanished_RemovesRow()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service);
        controller.Refresh();

        service.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        service.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var selection = controller.Refresh();

        Assert.Empty(controller.Projects);
        Assert.Null(selection);
    }

    [Fact]
    public void Refresh_NoCurrentProject_ReturnsNullSelection()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: null);
        var controller = this.CreateController(service);

        var selection = controller.Refresh();

        Assert.Single(controller.Projects);
        Assert.Null(selection);
    }

    [Fact]
    public void Refresh_EnrichesRowWithResolvedLogo()
    {
        var project = Project("project");
        var cache = new Mock<ILogoCacheService>();
        cache.Setup(x => x.GetOrResolve($"project:{project.Id}", It.IsAny<Func<string?>>())).Returns("logo.png");
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service, cache);

        controller.Refresh();

        Assert.Equal("logo.png", controller.Projects.Single().LogoPath);
    }

    [Fact]
    public void AddProject_AddsAndSelectsDialogFolder()
    {
        var added = Project("project");
        var projects = new List<Project>();
        var service = this.CreateService(projects, current: null);
        service.Setup(x => x.AddProject("C:\\Project")).Callback(() => projects.Add(added)).Returns(added);
        service.Setup(x => x.SetCurrentProject("project"));
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns("C:\\Project");
        var controller = this.CreateController(service, dialogs: dialogs);

        var addedResult = controller.AddProject();

        Assert.Equal(added.Id, addedResult?.Id);
        service.Verify(x => x.SetCurrentProject("project"), Times.Once);
        Assert.Equal(added.Id, controller.Projects.Single().Id);
    }

    [Fact]
    public void RemoveSelectedProject_WhenConfirmed_RemovesCurrentProject()
    {
        var project = Project("project");
        var projects = new List<Project> { project };
        var service = this.CreateService(projects, current: project);
        service.Setup(x => x.RemoveProject("project")).Callback(() => projects.Remove(project));
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns(project)
            .Returns(project)
            .Returns((Project?)null);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("project")).Returns(true);
        var controller = this.CreateController(service, dialogs: dialogs);
        controller.Refresh();

        Assert.True(controller.RemoveSelectedProject());
        service.Verify(x => x.RemoveProject("project"), Times.Once);
        Assert.Empty(controller.Projects);
    }

    [Fact]
    public void FindProject_ReturnsProjectById_AndNullForUnknownId()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service);

        Assert.Equal("project", controller.FindProject("project")?.Id);
        Assert.Null(controller.FindProject("unknown"));
    }

    [Fact]
    public void Dispose_UnsubscribesFromProjectChanges()
    {
        var service = this.CreateService([], current: null);
        var controller = this.CreateController(service);
        var changed = 0;
        controller.ProjectsChanged += (_, _) => changed++;

        controller.Dispose();
        service.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void AddCommand_ExecutesAddProjectWorkflow()
    {
        var added = Project("project");
        var projects = new List<Project>();
        var service = this.CreateService(projects, current: null);
        service.Setup(x => x.AddProject("C:\\Project")).Callback(() => projects.Add(added)).Returns(added);
        service.Setup(x => x.SetCurrentProject("project"));
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns("C:\\Project");
        var controller = this.CreateController(service, dialogs: dialogs);

        controller.AddCommand.Execute(null);

        Assert.Equal(added.Id, controller.Projects.Single().Id);
    }

    [Fact]
    public void RemoveCommand_CanExecuteOnlyWhenProjectSelected()
    {
        var project = Project("project");
        var service = this.CreateService([], current: null);
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns((Project?)null)
            .Returns(project);
        var controller = this.CreateController(service);

        Assert.False(controller.RemoveCommand.CanExecute(null));
        Assert.True(controller.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public void RemoveCommand_ExecutesRemoveSelectedProjectWorkflow()
    {
        var project = Project("project");
        var projects = new List<Project> { project };
        var service = this.CreateService(projects, current: project);
        service.Setup(x => x.RemoveProject("project")).Callback(() => projects.Remove(project));
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns(project)
            .Returns(project)
            .Returns((Project?)null);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("project")).Returns(true);
        var controller = this.CreateController(service, dialogs: dialogs);
        controller.Refresh();

        controller.RemoveCommand.Execute(null);

        Assert.Empty(controller.Projects);
    }

    [Fact]
    public void ToggleFavoriteCommand_CanExecuteOnlyWhenProjectSelected()
    {
        var project = Project("project");
        var service = this.CreateService([], current: null);
        service.SetupSequence(x => x.GetCurrentProject())
            .Returns((Project?)null)
            .Returns(project);
        var controller = this.CreateController(service);

        Assert.False(controller.ToggleFavoriteCommand.CanExecute(null));
        Assert.True(controller.ToggleFavoriteCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleFavoriteCommand_ExecutesToggleFavoriteWorkflow()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: project);
        service.Setup(x => x.ToggleFavorite("project"));
        var controller = this.CreateController(service);

        controller.ToggleFavoriteCommand.Execute(null);

        service.Verify(x => x.ToggleFavorite("project"), Times.Once);
    }

    [Fact]
    public void RefreshCommand_ExecutesRefreshWorkflow()
    {
        var project = Project("project");
        var service = this.CreateService([project], current: project);
        var controller = this.CreateController(service);

        controller.RefreshCommand.Execute(null);

        Assert.Equal(project.Id, controller.Projects.Single().Id);
    }

    private Mock<IProjectService> CreateService(List<Project> projects, Project? current)
    {
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetAllProjects()).Returns(() => (IReadOnlyList<Project>)projects.ToArray());
        service.Setup(x => x.GetCurrentProject()).Returns(current);
        return service;
    }

    private ProjectPaneController CreateController(
        Mock<IProjectService> service,
        Mock<ILogoCacheService>? cache = null,
        Mock<IProjectDialogService>? dialogs = null)
    {
        cache ??= new Mock<ILogoCacheService>();
        var enricher = new ProjectEnricher(new ProjectLogoResolver(cache.Object, new LogoOptions(null)));

        return new ProjectPaneController(
            service.Object,
            enricher,
            new ProjectModelCatalog(),
            dialogs?.Object ?? new Mock<IProjectDialogService>().Object,
            new PromptState());
    }

    private static Project Project(string id) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}"
    };
}
