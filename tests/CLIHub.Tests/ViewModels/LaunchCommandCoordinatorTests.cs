namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Models;
using CLIHub.ViewModels;

public class LaunchCommandCoordinatorTests
{
    [Fact]
    public async Task RunAsync_VersionSuccess_ReportsAndNotifiesAndRefreshes()
    {
        var item = CreateItem(AgentCommandKind.Version);
        var project = new Project { Id = "project", Name = "Project", Path = @"C:\Project" };
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(x => x.ExecuteAsync(item.Plugin, project, AgentCommandKind.Version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, "1.2.3", null));
        var notifications = new Mock<IUserNotificationService>(MockBehavior.Strict);
        notifications.Setup(x => x.ShowInformation("1.2.3", "Test Agent version"));
        var coordinator = CreateCoordinator(workflow.Object, notifications.Object);
        var status = new List<string>();
        var refreshCount = 0;

        await coordinator.RunAsync(item, project, AgentCommandKind.Version, status.Add, () => refreshCount++);

        Assert.Equal("Test Agent version: 1.2.3", status[^1]);
        Assert.Equal(1, refreshCount);
        notifications.VerifyAll();
        workflow.VerifyAll();
    }

    [Fact]
    public async Task RunAsync_MissingProject_ReportsWithoutExecuting()
    {
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        var coordinator = CreateCoordinator(workflow.Object);
        var status = new List<string>();

        await coordinator.RunAsync(CreateItem(AgentCommandKind.Launch), null, AgentCommandKind.Launch, status.Add, () => { });

        Assert.Equal("Select a project before running an agent command.", status.Single());
        workflow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_UnsupportedCommand_ReportsWithoutExecuting()
    {
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        var coordinator = CreateCoordinator(workflow.Object);
        var status = new List<string>();

        await coordinator.RunAsync(
            CreateItem(AgentCommandKind.Launch),
            new Project { Id = "project", Name = "Project", Path = @"C:\Project" },
            AgentCommandKind.Update,
            status.Add,
            () => { });

        Assert.Equal("Test Agent does not support the update command.", status.Single());
        workflow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_CommandFailure_ReportsFailureAndRefreshes()
    {
        var item = CreateItem(AgentCommandKind.Launch);
        var project = new Project { Id = "project", Name = "Project", Path = @"C:\Project" };
        var workflow = new Mock<IAgentCommandWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(x => x.ExecuteAsync(item.Plugin, project, AgentCommandKind.Launch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(false, null, "process failed"));
        var coordinator = CreateCoordinator(workflow.Object);
        var status = new List<string>();
        var refreshCount = 0;

        await coordinator.RunAsync(item, project, AgentCommandKind.Launch, status.Add, () => refreshCount++);

        Assert.Equal("Launch failed: process failed", status[^1]);
        Assert.Equal(1, refreshCount);
    }

    private static LaunchCommandCoordinator CreateCoordinator(
        IAgentCommandWorkflow workflow,
        IUserNotificationService? notifications = null)
    {
        var lifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        lifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(CancellationToken.None));
        return new LaunchCommandCoordinator(
            workflow,
            lifetime.Object,
            notifications ?? new Mock<IUserNotificationService>(MockBehavior.Strict).Object,
            NullLogger<LaunchCommandCoordinator>.Instance);
    }

    private static AgentItem CreateItem(AgentCommandKind kind)
    {
        var commands = new AgentCommands();
        var command = new PluginCommand { Executable = "test-agent" };
        switch (kind)
        {
            case AgentCommandKind.Launch:
                commands.Launch = command;
                break;
            case AgentCommandKind.Version:
                commands.Version = command;
                break;
            case AgentCommandKind.Update:
                commands.Update = command;
                break;
        }

        var plugin = new Plugin { Id = "test-agent", Name = "Test Agent", Commands = commands };
        return new AgentItem { Plugin = plugin, Name = plugin.Name };
    }
}
