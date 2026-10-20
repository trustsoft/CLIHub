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

    [Fact]
    public void GetCandidateProcessNames_CoversEveryNameTheMatcherAccepts()
    {
        // Arrange
        var plugins = new[]
        {
            CreatePlugin("opencode", "opencode.exe"),
            CreatePlugin("cline", @"C:\tools\cline.cmd"),
            CreatePlugin("pi", "pi"),
            PluginWithoutLaunchCommand()
        };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins);

        // Assert — every process the matcher would accept has its name in the candidate set.
        foreach (var plugin in plugins)
        {
            var executableName = Path.GetFileNameWithoutExtension(plugin.Commands?.Launch?.Executable ?? string.Empty);
            var process = CreateProcess($@"C:\somewhere\{executableName}.exe");

            if (this.matcher.IsMatch(process, plugin))
            {
                Assert.Contains(executableName, candidates);
            }
        }
    }

    [Fact]
    public void GetCandidateProcessNames_StripsExtensionAndIgnoresCase()
    {
        // Arrange
        var plugins = new[]
        {
            CreatePlugin("a", "TestAgent.exe"),
            CreatePlugin("b", "testagent"),
            CreatePlugin("c", "Other.cmd")
        };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins);

        // Assert — extensions stripped; compared ignoring case, so case variants collapse to one
        // entry (the set keeps the first-seen casing; membership is case-insensitive).
        Assert.Equal(2, candidates.Count);
        Assert.Contains("testagent", candidates);
        Assert.Contains("TESTAGENT", candidates);
        Assert.Contains("other", candidates);
        Assert.DoesNotContain("testagent.exe", candidates);
        Assert.DoesNotContain("other.cmd", candidates);
    }

    [Fact]
    public void GetCandidateProcessNames_PluginWithoutLaunchCommand_ContributesNothing()
    {
        // Arrange
        var plugins = new[] { PluginWithoutLaunchCommand() };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins);

        // Assert
        Assert.Empty(candidates);
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

    private static Plugin PluginWithoutLaunchCommand()
    {
        return new Plugin
        {
            Id = "no-launch",
            Name = "No Launch",
            Commands = new AgentCommands()
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
