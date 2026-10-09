namespace CLIHub.Core.Tests.Infrastructure.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AgentProcessMatcherTests
{
    private readonly AgentProcessMatcher _matcher;

    public AgentProcessMatcherTests()
    {
        _matcher = new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance);
    }

    [Fact]
    public void IsMatch_DirectExecutableMatch_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "testagent.exe");
        var process = CreateProcess(@"C:\tools\testagent.exe", "testagent.exe");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectExecutableMatchCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "TestAgent.exe");
        var process = CreateProcess(@"C:\tools\testagent.exe", "testagent.exe");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NodeRuntimeWithPluginIdInCommandLine_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\npm\opencode\index.js");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_PythonRuntimeWithExecutableNameInCommandLine_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("cline", "cline");
        var process = CreateProcess(
            @"C:\Python\python.exe",
            @"python.exe C:\scripts\cline\main.py");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NodeRuntimeWithSystemPathInCommandLine_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePluginWithSystemPath("opencode", "opencode", @"C:\Users\test\.npm\opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\test\.npm\opencode\bin\index.js");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DifferentExecutable_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "testagent.exe");
        var process = CreateProcess(@"C:\tools\other.exe", "other.exe");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_NodeRuntimeWithoutAgentMarkers_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\projects\myapp\server.js");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_PluginWithoutLaunchCommand_ReturnsFalse()
    {
        // Arrange
        var plugin = new Plugin
        {
            Id = "test-agent",
            Name = "Test Agent",
            Commands = null
        };
        var process = CreateProcess(@"C:\tools\test.exe", "test.exe");

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_EmptyCommandLine_ReturnsFalseForRuntimeProcess()
    {
        // Arrange
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\Program Files\nodejs\node.exe", string.Empty);

        // Act
        var result = _matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
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

    private static Plugin CreatePluginWithSystemPath(string id, string executable, string systemPath)
    {
        return new Plugin
        {
            Id = id,
            Name = id,
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = executable }
            },
            Detection = new AgentDetection
            {
                SystemPaths = new List<string> { systemPath }
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
