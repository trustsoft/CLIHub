namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class AgentProcessMonitorTests
{
    private readonly Mock<IAgentProcessInspector> mockInspector;
    private readonly Mock<IPluginCatalog> mockCatalog;
    private readonly AgentProcessMonitor monitor;

    public AgentProcessMonitorTests()
    {
        this.mockInspector = new Mock<IAgentProcessInspector>();
        this.mockCatalog = new Mock<IPluginCatalog>();
        this.monitor = new AgentProcessMonitor(
            CreateOrchestrator(this.mockInspector.Object),
            this.mockCatalog.Object,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            NullLogger<AgentProcessMonitor>.Instance);
    }

    // The monitor delegates to the orchestrator, whose matching semantics live in the real
    // AgentProcessFinder + AgentProcessMatcher over the mocked inspector.
    internal static IProcessOrchestrator CreateOrchestrator(IAgentProcessInspector inspector)
        => new ProcessOrchestrator(
            Mock.Of<IRuntimeResolver>(),
            Mock.Of<ICommandLineBuilder>(),
            Mock.Of<IInteractiveProcessRunner>(),
            Mock.Of<IProcessOutputRunner>(),
            new AgentProcessFinder(inspector, new AgentProcessMatcher(), new Mock<IPluginCatalog>().Object),
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            NullLogger<ProcessOrchestrator>.Instance);

    [Fact]
    public void HasRunningAgents_NoProcesses_ReturnsFalse()
    {
        // Arrange
        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = this.monitor.HasRunningAgents();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRunningAgents_NoMatchingProcesses_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\Windows\System32\notepad.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = this.monitor.HasRunningAgents();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRunningAgents_MatchingProcess_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = this.monitor.HasRunningAgents();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRunningAgents_PluginOverload_OtherAgentRunning_ReturnsFalse()
    {
        // Arrange
        var opencode = CreatePlugin("opencode", "opencode");
        var pi = CreatePlugin("pi", "pi");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([opencode, pi]);

        // Act
        var result = this.monitor.HasRunningAgents(pi);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRunningAgents_PluginOverload_SameAgentRunning_ReturnsTrue()
    {
        // Arrange
        var opencode = CreatePlugin("opencode", "opencode");
        var pi = CreatePlugin("pi", "pi");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([opencode, pi]);

        // Act
        var result = this.monitor.HasRunningAgents(opencode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetRunningAgents_PluginOverload_ReturnsOnlyRequestedAgent()
    {
        // Arrange
        var opencode = CreatePlugin("opencode", "opencode");
        var pi = CreatePlugin("pi", "pi");
        var opencodeProcess = CreateProcess(@"C:\tools\opencode.exe");
        var piProcess = CreateProcess(@"C:\tools\pi.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([opencodeProcess, piProcess]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([opencode, pi]);

        // Act
        var result = this.monitor.GetRunningAgents(pi);

        // Assert
        Assert.Single(result);
        Assert.Equal("pi", result[0].Plugin.Id);
    }

    [Fact]
    public void GetRunningAgents_NoProcesses_ReturnsEmpty()
    {
        // Arrange
        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRunningAgents_MatchingProcess_ReturnsRunningAgent()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Single(result);
        Assert.Equal(plugin.Id, result[0].Plugin.Id);
        Assert.Equal(process.ProcessId, result[0].Process.ProcessId);
    }

    [Fact]
    public void GetRunningAgents_MultipleMatchingProcesses_ReturnsAll()
    {
        // Arrange
        var plugin1 = CreatePlugin("opencode", "opencode");
        var plugin2 = CreatePlugin("cline", "cline");
        var process1 = CreateProcess(@"C:\tools\opencode.exe");
        var process2 = CreateProcess(@"C:\tools\cline.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process1, process2]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin1, plugin2]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Plugin.Id == "opencode");
        Assert.Contains(result, r => r.Plugin.Id == "cline");
    }

    [Fact]
    public void GetRunningAgents_MixedProcesses_ReturnsOnlyMatching()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var matchingProcess = CreateProcess(@"C:\tools\opencode.exe");
        var nonMatchingProcess = CreateProcess(@"C:\Windows\System32\notepad.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([matchingProcess, nonMatchingProcess]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Single(result);
        Assert.Equal("opencode", result[0].Plugin.Id);
    }

    [Fact]
    public void GetRunningAgents_InspectorThrows_ReturnsEmpty()
    {
        // Arrange
        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Throws<InvalidOperationException>();
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRunningAgents_PassesLaunchExecutableNamesToInspector()
    {
        // Arrange
        var opencode = CreatePlugin("opencode", "opencode");
        var cline = CreatePlugin("cline", "cline.cmd");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([opencode, cline]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert — the candidate set derives from the catalog's launch executables (extension
        // stripped), so the inspector pre-filter and the matcher share one comparison basis.
        this.mockInspector.Verify(
            x => x.GetRunningProcesses(It.Is<IReadOnlySet<string>>(names => names.Contains("opencode") && names.Contains("cline")), It.IsAny<AgentProcessMatchDetail>()),
            Times.Once);
        Assert.Single(result);
        Assert.Equal("opencode", result[0].Plugin.Id);
    }

    [Fact]
    public void GetRunningAgents_DefaultDetail_MatchesNodeShimCommandLine()
    {
        // Arrange
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");

        this.mockInspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act — the default tier is CommandLine, so the runtime-hosted agent matches.
        var result = this.monitor.GetRunningAgents();

        // Assert
        Assert.Single(result);
        Assert.Equal("opencode", result[0].Plugin.Id);
    }

    [Fact]
    public void GetRunningAgents_ExecutableNameDetail_DoesNotMatchNodeShim()
    {
        // Arrange
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");

        this.mockInspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act — the cheap tier compares executable names only; a host-named process cannot match.
        var result = this.monitor.GetRunningAgents(AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.Empty(result);
        this.mockInspector.Verify(
            x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), AgentProcessMatchDetail.ExecutableName),
            Times.Once);
    }

    [Fact]
    public void GetRunningAgents_MultiplePlugins_EnumeratesInspectorOnce()
    {
        // Arrange
        var plugin1 = CreatePlugin("opencode", "opencode");
        var plugin2 = CreatePlugin("cline", "cline");
        var process1 = CreateProcess(@"C:\tools\opencode.exe");
        var process2 = CreateProcess(@"C:\tools\cline.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process1, process2]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin1, plugin2]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert — the catalog-wide query enumerates processes once regardless of plugin count.
        this.mockInspector.Verify(
            x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()),
            Times.Once);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetRunningAgents_PreservesExecutablePathInResult()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe");

        this.mockInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([process]);
        this.mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = this.monitor.GetRunningAgents();

        // Assert — the domain mapping must not lose the executable path.
        Assert.Single(result);
        Assert.Equal(@"C:\tools\opencode.exe", result[0].Process.ExecutablePath);
    }

    private static Plugin CreatePlugin(string id, string executable)
    {
        return new Plugin
        {
            Id = id,
            Name = id,
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = executable }
            }
        };
    }

    private static Plugin CreateNpmPlugin(string id, string executable, string package)
    {
        var plugin = CreatePlugin(id, executable);
        plugin.UpdateCheck = new PluginUpdateCheck("npm", package);
        return plugin;
    }

    private static AgentProcessInstance CreateProcess(string executablePath, string? commandLine = null)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            StartTime: DateTime.UtcNow,
            CommandLine: commandLine);
    }
}
