namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

public class AgentManagementServiceTests
{
    private static Plugin AgentPlugin(string id = "opencode", bool withUpdate = true) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = id },
            Update = withUpdate ? new PluginCommand { Executable = id, Arguments = "update" } : null
        }
    };

    private static AgentProcessMonitor Monitor(Plugin[] plugins, params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector.Setup(x => x.GetRunningProcesses()).Returns(processes);

        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        return new AgentProcessMonitor(
            inspector.Object,
            new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance),
            catalog.Object,
            NullLogger<AgentProcessMonitor>.Instance);
    }

    private static AgentManagementService CreateService(
        Mock<IAgentDetectionService> detection,
        Mock<IAgentVersionService> version,
        Mock<IAgentCommandService> commands,
        AgentProcessMonitor monitor) =>
        new(detection.Object, version.Object, commands.Object, monitor, NullLogger<AgentManagementService>.Instance);

    [Fact]
    public async Task ProbeAsync_ReturnsInstalledStateAndVersion()
    {
        var plugin = AgentPlugin();
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(plugin)).Returns(true);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>())).ReturnsAsync("1.2.3");
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]));

        var result = await service.ProbeAsync(plugin);

        Assert.True(result.IsInstalled);
        Assert.Equal("1.2.3", result.Version);
        detection.Verify(x => x.Invalidate(), Times.Once);
        version.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task ProbeAsync_UnknownVersion_ReturnsNullVersion()
    {
        var plugin = AgentPlugin();
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(plugin)).Returns(false);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]));

        var result = await service.ProbeAsync(plugin);

        Assert.False(result.IsInstalled);
        Assert.Null(result.Version);
    }

    [Fact]
    public async Task UpdateAsync_NoUpdateCommand_IsUnsupported()
    {
        var plugin = AgentPlugin(withUpdate: false);
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Unsupported, result.Outcome);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_RunningAgent_IsBlocked()
    {
        var plugin = AgentPlugin();
        var process = new AgentProcessInstance(1234, @"C:\tools\opencode.exe", "opencode.exe", DateTime.UtcNow);
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            Monitor([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_NoRunningAgent_StartsUpdate()
    {
        var plugin = AgentPlugin();
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.True(result.Started);
    }

    [Fact]
    public async Task UpdateAsync_CommandFailure_IsFailed()
    {
        var plugin = AgentPlugin();
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(false, null, "boom"));
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Failed, result.Outcome);
        Assert.Equal("boom", result.Message);
    }
}
