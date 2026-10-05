namespace CLIHub.Tests.Services;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Models;

public class AgentCommandWorkflowTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsPluginProjectPathKindAndCancellation()
    {
        var plugin = new Plugin
        {
            Id = "opencode",
            Name = "OpenCode",
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = "opencode" }
            }
        };
        var project = new Project { Id = "project", Name = "Project", Path = @"C:\Projects\Project" };
        using var cancellation = new CancellationTokenSource();
        var expected = new AgentCommandResult(true, null, null);
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Launch, project.Path, cancellation.Token))
            .ReturnsAsync(expected);
        var workflow = new AgentCommandWorkflow(commands.Object);

        var result = await workflow.ExecuteAsync(plugin, project, AgentCommandKind.Launch, cancellation.Token);

        Assert.Equal(expected, result);
        commands.VerifyAll();
    }
}
