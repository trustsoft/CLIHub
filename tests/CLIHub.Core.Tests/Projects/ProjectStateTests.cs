namespace CLIHub.Core.Tests.Projects;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class ProjectStateTests
{
    [Fact]
    public void From_AppConfig_MutatingSourceAfterwards_StateKeepsOriginalValues()
    {
        var config = new AppConfig();
        config.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });

        var state = ProjectState.From(config);
        config.Projects[0].IsFavorite = true;

        Assert.False(state.Projects[0].IsFavorite);
    }

    [Fact]
    public void From_Snapshot_MutatingSourceAfterwards_StateKeepsOriginalValues()
    {
        var snapshot = new ConfigurationSnapshot();
        snapshot.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });

        var state = ProjectState.From(snapshot);
        snapshot.Projects[0].IsFavorite = true;

        Assert.False(state.Projects[0].IsFavorite);
    }

    [Fact]
    public void ApplyTo_AppConfig_MutatingStateAfterwards_ConfigKeepsOriginalValues()
    {
        var state = new ProjectState();
        state.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });
        var config = new AppConfig();

        state.ApplyTo(config);
        state.Projects[0].IsFavorite = true;

        Assert.False(config.Projects[0].IsFavorite);
    }

    [Fact]
    public void ApplyTo_Snapshot_MutatingStateAfterwards_SnapshotKeepsOriginalValues()
    {
        var state = new ProjectState();
        state.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });
        var snapshot = new ConfigurationSnapshot();

        state.ApplyTo(snapshot);
        state.Projects[0].IsFavorite = true;

        Assert.False(snapshot.Projects[0].IsFavorite);
    }

    [Fact]
    public void CopyTo_Target_MutatingSourceAfterwards_TargetKeepsOriginalValues()
    {
        var source = new ProjectState();
        source.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = false
        });
        var target = new ProjectState();

        source.CopyTo(target);
        source.Projects[0].IsFavorite = true;

        Assert.False(target.Projects[0].IsFavorite);
    }
}
