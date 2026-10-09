namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Tests.Fakes;

public class AgentVersionServiceTests
{
    private readonly FakeProcessOutputRunner _outputRunner = new();
    private readonly ProcessLauncher _launcher;
    private readonly FakeConfigurationRepository _config = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    public AgentVersionServiceTests()
    {
        _launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new RuntimeSelector(),
            new FakeInteractiveProcessRunner(),
            _outputRunner);
    }

    private AgentVersionService CreateService() =>
        new(
            _launcher,
            new PreferencesStore(_config),
            NullLogger<AgentVersionService>.Instance,
            _time);

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
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3\nsome banner",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("1.2.3", version);
        Assert.Single(_outputRunner.Runs);
    }

    [Theory]
    [InlineData("GitHub Copilot CLI 1.0.88.", "1.0.88")]
    [InlineData("0.30.0 (OpenClaude)", "0.30.0")]
    [InlineData("1.18.32", "1.18.32")]
    [InlineData("v2.5", "2.5")]
    public async Task GetVersion_ExtractsVersionNumber(string output, string expected)
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = output,
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal(expected, version);
    }

    [Fact]
    public async Task GetVersion_NoVersionNumber_ReturnsLineAsIs()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "no digits here",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("no digits here", version);
    }

    [Fact]
    public async Task GetVersion_NoVersionCommand_ReturnsNullAndDoesNotRun()
    {
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(false));

        Assert.Null(version);
        Assert.Empty(_outputRunner.Runs);
    }

    [Fact]
    public async Task GetVersion_CaptureFailure_ReturnsNull()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = string.Empty,
            StandardError = "Command not found",
            ExitCode = 1,
            TimedOut = false
        };
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_Timeout_ReturnsNull()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = string.Empty,
            StandardError = string.Empty,
            ExitCode = -1,
            TimedOut = true
        };
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_WithinCacheTtl_ReturnsCachedVersionWithoutRunning()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        _time.Advance(TimeSpan.FromMinutes(10));
        var cached = await service.GetVersionAsync(plugin);

        Assert.Equal("1.2.3", cached);
        Assert.Single(_outputRunner.Runs); // Only ran once
    }

    [Fact]
    public async Task GetVersion_AfterCacheExpiry_RunsAgain()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        _time.Advance(TimeSpan.FromMinutes(20)); // Default TTL is 15 minutes
        
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "2.0.0",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };

        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        Assert.Equal(2, _outputRunner.Runs.Count); // Ran twice
    }

    [Fact]
    public async Task Invalidate_ClearsCachedVersions()
    {
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "1.2.3",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        var service = CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        service.Invalidate();
        
        _outputRunner.Result = new ProcessResult
        {
            StandardOutput = "2.0.0",
            StandardError = string.Empty,
            ExitCode = 0,
            TimedOut = false
        };
        
        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        Assert.Equal(2, _outputRunner.Runs.Count); // Ran twice after invalidation
    }
}
