namespace CLIHub.Tests.ViewModels;

using System.Windows.Input;

using Moq;

using CLIHub.ViewModels;

public class LaunchWindowActionBuilderTests
{
    [Fact]
    public void Build_PreservesPaneActionsAndFilterState()
    {
        var commands = Enumerable.Range(0, 9)
            .Select(_ => new Mock<ICommand>(MockBehavior.Strict).Object)
            .ToArray();
        var actions = new LaunchWindowActionBuilder().Build(
            commands[0], commands[1], commands[2], commands[3], commands[4],
            commands[5], commands[6], commands[7], commands[8], true);

        Assert.Equal(["Add Project...", "Remove Project", "Toggle Favorite", "Refresh"],
            actions.Projects.Select(action => action.Label));
        Assert.Equal(
            ["Launch", "Resume Session", "Initialize", "Update", "Show Version", string.Empty,
             "Only agents available in project", string.Empty, "Refresh"],
            actions.Agents.Select(action => action.Label));
        Assert.Same(actions.FilterAction, actions.Agents[6]);
        Assert.True(actions.FilterAction.IsChecked);
        Assert.Same(commands[0], actions.Projects[0].Command);
        Assert.Same(commands[8], actions.Agents[4].Command);
    }
}
