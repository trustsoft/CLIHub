namespace CLIHub.Core.Tests.Infrastructure.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using Xunit;

public class AgentProcessMatcherTests
{
    private readonly AgentProcessMatcher matcher;

    public AgentProcessMatcherTests()
    {
        this.matcher = new AgentProcessMatcher();
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
    public void IsMatch_RuntimeHostedProcessWithoutCommandLine_ReturnsFalse()
    {
        // Arrange
        // An npm-distributed agent can run inside a shared node.exe behind a .cmd shim. A
        // host-named process with no command line (retrieval degraded or unavailable) still
        // deliberately does not match: the command-line rule requires a readable command line
        // carrying the declared npm package (design D5).
        var plugin = CreatePlugin("opencode", "opencode");
        var process = CreateProcess(@"C:\Program Files\nodejs\node.exe");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_NodeShimCommandLineOfDeclaredNpmPackage_ReturnsTrue()
    {
        // Arrange
        // The backlog's exact shape: node.exe started by the agent's npm shim, its command line
        // naming the declared package under node_modules.
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_UnscopedNpmPackage_ReturnsTrue()
    {
        // Arrange
        var plugin = CreateNpmPlugin("cline-cli", "cline", "cline");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe ""C:\Program Files\nodejs\node_modules\cline\dist\index.js""");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ForwardSlashCommandLine_ReturnsTrue()
    {
        // Arrange
        // Node normalizes paths with forward slashes; the command line is normalized before the
        // marker comparison.
        var plugin = CreateNpmPlugin("qwen-code", "qwen", "@qwen-code/qwen-code");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:/Users/x/AppData/Roaming/npm/node_modules/@qwen-code/qwen-code/bin/qwen.js");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_CommandLineCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@OPENCODE\CLI\BIN\OPENCODE.JS run");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_SiblingPackageWithCommonPrefix_ReturnsFalse()
    {
        // Arrange
        // The trailing separator is the boundary: @opencode/cli must not match inside
        // @opencode/cli-extra.
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli-extra\bin\x.js");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_UnrelatedNodeScript_ReturnsFalse()
    {
        // Arrange
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var relativeScript = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe server.js");
        var absoluteScript = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe D:\build\script.js");

        // Act
        var relativeResult = this.matcher.IsMatch(relativeScript, plugin);
        var absoluteResult = this.matcher.IsMatch(absoluteScript, plugin);

        // Assert
        Assert.False(relativeResult);
        Assert.False(absoluteResult);
    }

    [Fact]
    public void IsMatch_PluginWithoutNpmUpdateCheck_ReturnsFalse()
    {
        // Arrange
        // A githubReleases-distributed agent keeps executable-name matching only; a node process
        // naming some package never matches it.
        var plugin = CreateGithubReleasesPlugin("github-copilot-cli", "copilot");
        var process = CreateProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\npm\node_modules\github-copilot-cli\bin\copilot.js");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_EmptyCommandLine_ReturnsFalse()
    {
        // Arrange
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(@"C:\Program Files\nodejs\node.exe", string.Empty);

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_RuntimeHostProcessWithNpmPackageUnderPythonHost_ReturnsTrue()
    {
        // Arrange
        // python-hosted entry points match the same rule through the python host names.
        var plugin = CreateNpmPlugin("opencode", "opencode", "@opencode/cli");
        var process = CreateProcess(
            @"C:\Python312\python.exe",
            @"python.exe C:\tools\node_modules\@opencode\cli\bin\opencode.js");

        // Act
        var result = this.matcher.IsMatch(process, plugin);

        // Assert
        Assert.True(result);
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

    [Fact]
    public void GetCandidateProcessNames_CommandLineDetail_AddsRuntimeHostsWithNpmPlugin()
    {
        // Arrange
        var plugins = new[]
        {
            CreateNpmPlugin("opencode", "opencode", "@opencode/cli"),
            CreatePlugin("copilot", "copilot")
        };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins, AgentProcessMatchDetail.CommandLine);

        // Assert — runtime hosts sit beside the launch names exactly when command-line matching
        // is active for an npm-declared catalog.
        Assert.Contains("opencode", candidates);
        Assert.Contains("copilot", candidates);
        Assert.Contains("node", candidates);
        Assert.Contains("python", candidates);
        Assert.Contains("pythonw", candidates);
    }

    [Fact]
    public void GetCandidateProcessNames_ExecutableNameDetail_AddsNoRuntimeHosts()
    {
        // Arrange
        var plugins = new[] { CreateNpmPlugin("opencode", "opencode", "@opencode/cli") };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins, AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.Contains("opencode", candidates);
        Assert.DoesNotContain("node", candidates);
        Assert.DoesNotContain("python", candidates);
        Assert.DoesNotContain("pythonw", candidates);
    }

    [Fact]
    public void GetCandidateProcessNames_NoNpmPlugin_AddsNoRuntimeHosts()
    {
        // Arrange — a launch-only catalog with no npm updateCheck never gains host candidates,
        // even under CommandLine detail.
        var plugins = new[] { CreatePlugin("copilot", "copilot") };

        // Act
        var candidates = this.matcher.GetCandidateProcessNames(plugins, AgentProcessMatchDetail.CommandLine);

        // Assert
        Assert.Contains("copilot", candidates);
        Assert.DoesNotContain("node", candidates);
        Assert.DoesNotContain("python", candidates);
        Assert.DoesNotContain("pythonw", candidates);
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

    private static Plugin CreateGithubReleasesPlugin(string id, string executable)
    {
        var plugin = CreatePlugin(id, executable);
        plugin.UpdateCheck = new PluginUpdateCheck("githubReleases", Repo: "owner/repo");
        return plugin;
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

    private static AgentProcessInstance CreateProcess(string executablePath, string? commandLine = null)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            StartTime: DateTime.UtcNow,
            CommandLine: commandLine);
    }
}
