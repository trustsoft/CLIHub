namespace CLIHub.Core.Tests.Services;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class AgentProcessMonitorTests
{
    private readonly Mock<IAgentProcessInspector> _mockInspector;
    private readonly Mock<IPluginCatalog> _mockCatalog;
    private readonly CLIHub.Core.Infrastructure.Processes.AgentProcessMatcher _matcher;
    private readonly AgentProcessMonitor _monitor;

    public AgentProcessMonitorTests()
    {
        _mockInspector = new Mock<IAgentProcessInspector>();
        _mockCatalog = new Mock<IPluginCatalog>();
        _matcher = new CLIHub.Core.Infrastructure.Processes.AgentProcessMatcher(
            NullLogger<CLIHub.Core.Infrastructure.Processes.AgentProcessMatcher>.Instance);
        _monitor = new AgentProcessMonitor(
            _mockInspector.Object,
            _matcher,
            _mockCatalog.Object,
            NullLogger<AgentProcessMonitor>.Instance);
    }

    [Fact]
    public void HasRunningAgents_NoProcesses_ReturnsFalse()
    {
        // Arrange
        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = _monitor.HasRunningAgents();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRunningAgents_NoMatchingProcesses_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\Windows\System32\notepad.exe", "notepad.exe");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([process]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = _monitor.HasRunningAgents();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRunningAgents_MatchingProcess_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe", "opencode.exe");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([process]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = _monitor.HasRunningAgents();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetRunningAgents_NoProcesses_ReturnsEmpty()
    {
        // Arrange
        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = _monitor.GetRunningAgents();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRunningAgents_MatchingProcess_ReturnsRunningAgent()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\tools\opencode.exe", "opencode.exe");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([process]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = _monitor.GetRunningAgents();

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
        var process1 = CreateProcess(@"C:\tools\opencode.exe", "opencode.exe");
        var process2 = CreateProcess(@"C:\tools\cline.exe", "cline.exe");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([process1, process2]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin1, plugin2]);

        // Act
        var result = _monitor.GetRunningAgents();

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
        var matchingProcess = CreateProcess(@"C:\tools\opencode.exe", "opencode.exe");
        var nonMatchingProcess = CreateProcess(@"C:\Windows\System32\notepad.exe", "notepad.exe");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([matchingProcess, nonMatchingProcess]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = _monitor.GetRunningAgents();

        // Assert
        Assert.Single(result);
        Assert.Equal("opencode", result[0].Plugin.Id);
    }

    [Fact]
    public void GetRunningAgents_InspectorThrows_ReturnsEmpty()
    {
        // Arrange
        _mockInspector.Setup(x => x.GetRunningProcesses()).Throws<InvalidOperationException>();
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([]);

        // Act
        var result = _monitor.GetRunningAgents();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRunningAgents_NodeRuntimeProcess_ReturnsMatch()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\npm\opencode\index.js");

        _mockInspector.Setup(x => x.GetRunningProcesses()).Returns([process]);
        _mockCatalog.Setup(x => x.GetAllPlugins()).Returns([plugin]);

        // Act
        var result = _monitor.GetRunningAgents();

        // Assert
        Assert.Single(result);
        Assert.Equal("opencode", result[0].Plugin.Id);
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

    private static AgentProcessInstance CreateProcess(string executablePath, string commandLine)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            CommandLine: commandLine,
            StartTime: DateTime.UtcNow);
    }
}
