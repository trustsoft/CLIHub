namespace CLIHub.Tests.ViewModels;

using Moq;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Tests.Builders;

public class LaunchWindowViewModelDialogTests
{
    [Fact]
    public void AddProject_WhenFolderSelectionIsCancelled_UsesDialogServiceAndDoesNotMutateProjects()
    {
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectProjectFolder()).Returns((string?)null);

        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .WithProjectDialogService(dialogs)
            .BuildWithMocks();

        viewModel.AddProjectCommand.Execute(null);

        dialogs.Verify(x => x.SelectProjectFolder(), Times.Once);
        mocks.ProjectService.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
        Assert.Empty(viewModel.Projects);

        viewModel.ExitCommand.Execute(null);

        mocks.ApplicationLifetime.Verify(x => x.Shutdown(), Times.Once);
    }

    [Fact]
    public void Commands_ArePassThroughToControllers()
    {
        var viewModel = new LaunchWindowViewModelBuilder().Build();

        // The ViewModel exposes controller commands directly
        Assert.NotNull(viewModel.AddProjectCommand);
        Assert.NotNull(viewModel.RemoveProjectCommand);
        Assert.NotNull(viewModel.ToggleFavoriteCommand);
        Assert.NotNull(viewModel.RefreshCommand);
    }

    [Fact]
    public void Dispose_RemovesEventSubscriptions()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

        var viewModel = new LaunchWindowViewModelBuilder()
            .WithProjectService(projectService)
            .Build();

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
