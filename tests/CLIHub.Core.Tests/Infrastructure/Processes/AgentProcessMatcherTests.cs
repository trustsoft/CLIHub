namespace CLIHub.Core.Tests.Infrastructure.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AgentProcessMatcherTests
{
    private readonly AgentProcessMatcher matcher;

    public AgentProcessMatcherTests()
    {
        this.matcher = new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance);
    }

    [Fact]
    public void IsMatch_DirectExecutableMatch_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "testagent.exe");
        var process = CreateProcess(@"C:\tools\testagent.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectExecutableMatchCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "TestAgent.exe");
        var process = CreateProcess(@"C:\tools\testagent.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DifferentExecutable_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePlugin("test-agent", "testagent.exe");
        var process = CreateProcess(@"C:\tools\other.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

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
            Commands = new AgentCommands()
        };
        var process = CreateProcess(@"C:\tools\test.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_RuntimeHostedProcess_ReturnsFalse()
    {
        // Arrange
        // An npm-distributed agent can run inside a shared node.exe behind a .cmd shim. The
        // inspector does not retrieve command lines, so that process deliberately does not match.
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\Program Files\nodejs\node.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

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

    private static AgentProcessInstance CreateProcess(string executablePath)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            StartTime: DateTime.UtcNow);
    }
}
