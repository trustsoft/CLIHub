namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Tests.Fakes;

public class AgentVersionServiceTests
{
    private readonly FakeProcessOutputRunner outputRunner = new();
    private readonly ProcessLauncher launcher;
    private readonly FakeConfigurationRepository config = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    public AgentVersionServiceTests()
    {
        this.launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new RuntimeSelector(),
            new FakeInteractiveProcessRunner(),
            this.outputRunner);
    }

    private AgentVersionService CreateService() =>
        new(
            this.launcher,
            new PreferencesStore(this.config),
            NullLogger<AgentVersionService>.Instance,
            this.time);

    private static Plugin PluginWithVersion(bool includeVersion)
    {
        var commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = "agent" }
        };
        if (includeVersion)
        {
            commands.Version = new PluginCommand { Executable = "agent", Arguments = "--version" };
        }

        return new Plugin { Id = "agent", Name = "Agent", Commands = commands };
    }

    [Fact]
    public async Task GetVersion_CommandDefined_ReturnsFirstLine()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3\nsome banner",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("1.2.3", version);
        Assert.Single(this.outputRunner.Runs);
    }

    [Theory]
    [InlineData("GitHub Copilot CLI 1.0.88.", "1.0.88")]
    [InlineData("0.30.0 (OpenClaude)", "0.30.0")]
    [InlineData("1.18.32", "1.18.32")]
    [InlineData("v2.5", "2.5")]
    public async Task GetVersion_ExtractsVersionNumber(string output, string expected)
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = output,
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal(expected, version);
    }

    [Fact]
    public async Task GetVersion_NoVersionNumber_ReturnsLineAsIs()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "no digits here",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("no digits here", version);
    }

    [Fact]
    public async Task GetVersion_NoVersionCommand_ReturnsNullAndDoesNotRun()
    {
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(false));

        Assert.Null(version);
        Assert.Empty(this.outputRunner.Runs);
    }

    [Fact]
    public async Task GetVersion_CaptureFailure_ReturnsNull()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = string.Empty,
            StandardError = "Command not found",
            ExitCode = 1,
            TimedOut = false
        };
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_Timeout_ReturnsNull()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = string.Empty,
            StandardError = string.Empty,
            ExitCode = -1,
            TimedOut = true
        };
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_WithinCacheTtl_ReturnsCachedVersionWithoutRunning()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(10));
        var cached = await service.GetVersionAsync(plugin);

        Assert.Equal("1.2.3", cached);
        Assert.Single(this.outputRunner.Runs); // Only ran once
    }

    [Fact]
    public async Task GetVersion_AfterCacheExpiry_RunsAgain()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(20)); // Default TTL is 15 minutes

        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "2.0.0",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };

        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        Assert.Equal(2, this.outputRunner.Runs.Count); // Ran twice
    }

    [Fact]
    public async Task Invalidate_ClearsCachedVersions()
    {
        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        service.Invalidate();

        this.outputRunner.Result = new ProcessResult
        {
            StandardOutput = "2.0.0",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        
        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        Assert.Equal(2, this.outputRunner.Runs.Count); // Ran twice after invalidation
    }
}
