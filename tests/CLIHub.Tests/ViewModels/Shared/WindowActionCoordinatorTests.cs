namespace CLIHub.Tests.ViewModels.Shared;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.Core.Projects;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Updates;
using CLIHub.ViewModels;

using Xunit;

public class WindowActionCoordinatorTests
{
    [Fact]
    public void CanExecuteUpdate_NameMatchedAgent_ReturnsFalse()
    {
        var plugin = NpmAgentPlugin("opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe", null);
        var coordinator = CreateCoordinator([plugin], [process], out _, out _, out _);

        // Act
        var canExecute = coordinator.UpdateCommand.CanExecute(null);

        // Assert — a directly matched agent process blocks the advisory check itself.
        Assert.False(canExecute);
    }

    [Fact]
    public void CanExecuteUpdate_NodeShimAgent_QueriesExecutableNameDetailOnly()
    {
        var plugin = NpmAgentPlugin("opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");
        var coordinator = CreateCoordinator([plugin], [process], out var inspector, out _, out _);

        // Act — the requery path must consult the cheap tier only; the runtime-hosted process
        // cannot even match under it.
        var canExecute = coordinator.UpdateCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
        inspector.Verify(
            x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), AgentProcessMatchDetail.ExecutableName),
            Times.Once);
        inspector.Verify(
            x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), AgentProcessMatchDetail.CommandLine),
            Times.Never);
    }

    [Fact]
    public void ExecuteUpdate_NodeShimAgentRunning_BlocksWithoutLaunching()
    {
        var plugin = NpmAgentPlugin("opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");
        var coordinator = CreateCoordinator([plugin], [process], out var inspector, out var status, out var workflow);

        // Act
        coordinator.UpdateCommand.Execute(null);

        // Assert — the authoritative full-tier gate reports the block; the external launch path
        // never starts.
        var blocked = SpinWait.SpinUntil(
            () => status.CurrentMessage.Contains("Cannot update while the agent is running", StringComparison.Ordinal),
            TimeSpan.FromSeconds(5));
        Assert.True(blocked, $"Expected the blocked status, got '{status.CurrentMessage}'");
        Assert.Contains("opencode", status.CurrentMessage, StringComparison.Ordinal);
        workflow.Verify(
            x => x.ExecuteAsync(It.IsAny<Plugin>(), It.IsAny<Project>(), It.IsAny<AgentCommandKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
        inspector.Verify(
            x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), AgentProcessMatchDetail.CommandLine),
            Times.Once);
    }

    [Fact]
    public void ExecuteUpdate_NoAgentRunning_StartsUpdate()
    {
        var plugin = NpmAgentPlugin("opencode");
        var coordinator = CreateCoordinator([plugin], [], out _, out var status, out var workflow);

        // Act
        coordinator.UpdateCommand.Execute(null);

        // Assert — async settle with a bounded SpinWait until the launch flow reports the start.
        var started = SpinWait.SpinUntil(
            () => status.CurrentMessage == "Update started for opencode",
            TimeSpan.FromSeconds(5));
        Assert.True(started, $"Expected the started status, got '{status.CurrentMessage}'");
        workflow.Verify(
            x => x.ExecuteAsync(plugin, It.IsAny<Project>(), AgentCommandKind.Update, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static WindowActionCoordinator CreateCoordinator(
        Plugin[] plugins,
        AgentProcessInstance[] processes,
        out Mock<IAgentProcessInspector> inspector,
        out StatusMessageCoordinator status,
        out Mock<IAgentCommandWorkflow> workflow)
    {
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns(processes);

        // The monitor matches through the real finder + matcher over the mocked inspector, so the
        // tier semantics (executable-name vs command-line) stay in force.
        var monitor = new AgentProcessMonitor(
            new ProcessOrchestrator(
                Mock.Of<IRuntimeResolver>(),
                Mock.Of<ICommandLineBuilder>(),
                Mock.Of<IInteractiveProcessRunner>(),
                Mock.Of<IProcessOutputRunner>(),
                new AgentProcessFinder(inspector.Object, new AgentProcessMatcher(), catalog.Object),
                new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
                NullLogger<ProcessOrchestrator>.Instance),
            catalog.Object,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            NullLogger<AgentProcessMonitor>.Instance);

        workflow = new Mock<IAgentCommandWorkflow>();
        workflow
            .Setup(x => x.ExecuteAsync(It.IsAny<Plugin>(), It.IsAny<Project>(), AgentCommandKind.Update, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));

        var launchCoordinator = new LaunchCommandCoordinator(
            workflow.Object,
            new InlineOperationLifetime(),
            new Mock<IUserNotificationService>().Object,
            NullLogger<LaunchCommandCoordinator>.Instance);

        status = CreateStatusCoordinator();

        var agentItem = new AgentItem { Plugin = plugins[0], Name = plugins[0].Name };
        var project = new Project { Id = "proj", Name = "proj", Path = @"D:\code\proj" };

        return new WindowActionCoordinator(
            launchCoordinator,
            status,
            monitor,
            new Mock<ISettingsLauncher>().Object,
            new Mock<IApplicationLifetime>().Object,
            new Mock<IExternalLauncher>().Object,
            () => agentItem,
            () => project,
            () => { });
    }

    private static StatusMessageCoordinator CreateStatusCoordinator()
    {
        var projectService = new Mock<IProjectService>();
        projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
        projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);
        var projectPane = new ProjectPaneController(
            projectService.Object,
            new ProjectEnricher(new ProjectLogoResolver(new Mock<ILogoCacheService>().Object, new LogoOptions(null))),
            new ProjectModelCatalog(),
            new Mock<IProjectDialogService>().Object,
            new PromptState());
        var pluginCatalog = new Mock<IPluginCatalog>();
        pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());
        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            new Mock<IAgentDetectionService>().Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            new Mock<IApplicationOperationLifetime>().Object,
            NullLogger<AgentPaneController>.Instance);
        var updates = new Mock<IUpdateWorkflow>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        var updateControl = new UpdateControlViewModel(
            updates.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            new Mock<IApplicationOperationLifetime>().Object);
        return new StatusMessageCoordinator(projectPane, agentPane, updateControl);
    }

    private static Plugin NpmAgentPlugin(string id)
    {
        return new Plugin
        {
            Id = id,
            Name = id,
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = id },
                Update = new PluginCommand { Executable = id, Arguments = "update" }
            },
            UpdateCheck = new PluginUpdateCheck("npm", "@opencode/cli")
        };
    }

    private static AgentProcessInstance CreateProcess(string executablePath, string? commandLine)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            StartTime: DateTime.UtcNow,
            CommandLine: commandLine);
    }

    private sealed class InlineOperationLifetime : IApplicationOperationLifetime
    {
        public CancellationToken Token => CancellationToken.None;

        public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
        {
            var task = operation(CancellationToken.None);
            task.GetAwaiter().GetResult();
            return task;
        }

        public Task StopAsync(TimeSpan timeout) => Task.CompletedTask;
    }
}
