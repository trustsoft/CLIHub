namespace CLIHub.Tests.Builders;

using CLIHub.Core.Models;

public class LaunchWindowViewModelBuilderTests
{
    [Fact]
    public void Build_WithDefaults_CreatesViewModelWithEmptyProjects()
    {
        var viewModel = new LaunchWindowViewModelBuilder()
            .Build();

        Assert.NotNull(viewModel);
        Assert.Empty(viewModel.Projects);
        Assert.Null(viewModel.SelectedProject);
    }

    [Fact]
    public void Build_WithProjects_CreatesViewModelWithProjects()
    {
        var project1 = new Project { Id = "1", Name = "Project1", Path = "/path/to/project1", IsFavorite = false };
        var project2 = new Project { Id = "2", Name = "Project2", Path = "/path/to/project2", IsFavorite = true };

        var viewModel = new LaunchWindowViewModelBuilder()
            .WithProjects(project1, project2)
            .Build();

        Assert.Equal(2, viewModel.Projects.Count);
    }

    [Fact]
    public void Build_WithCurrentProject_SetsSelectedProject()
    {
        var project = new Project { Id = "1", Name = "TestProject", Path = "/path/to/project", IsFavorite = false };

        var viewModel = new LaunchWindowViewModelBuilder()
            .WithProjects(project)
            .WithCurrentProject(project)
            .Build();

        Assert.NotNull(viewModel.SelectedProject);
        Assert.Equal("TestProject", viewModel.SelectedProject.Name);
    }

    [Fact]
    public void BuildWithMocks_ReturnsViewModelAndMocks()
    {
        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .BuildWithMocks();

        Assert.NotNull(viewModel);
        Assert.NotNull(mocks);
        Assert.NotNull(mocks.ProjectService);
        Assert.NotNull(mocks.PluginCatalog);
        Assert.NotNull(mocks.ApplicationLifetime);
    }

    [Fact]
    public void BuildWithMocks_AllowsMockVerification()
    {
        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .BuildWithMocks();

        viewModel.ExitCommand.Execute(null);

        mocks.ApplicationLifetime.Verify(x => x.Shutdown(), Moq.Times.Once);
    }
}
