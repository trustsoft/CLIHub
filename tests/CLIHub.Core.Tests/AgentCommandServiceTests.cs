namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Tests.Fakes;

public class AgentCommandServiceTests : IDisposable
{
    private readonly string _projectDir;
    private readonly FakeInteractiveProcessRunner _interactiveRunner = new();
    private readonly FakeProcessOutputRunner _outputRunner = new();
    private readonly ProcessLauncher _launcher;
    private readonly AgentCommandService _service;

    public AgentCommandServiceTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "clihub-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDir);
        
        _launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new RuntimeSelector(),
            _interactiveRunner,
            _outputRunner);
        
        _service = new AgentCommandService(
            _launcher,
            NullLogger<AgentCommandService>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_projectDir, recursive: true); } catch { }
    }

    private static Plugin PluginWith(AgentCommands commands) =>
        new() { Id = "test", Name = "Test", Commands = commands };

    [Fact]
    public async Task Version_CapturesOutput_AndDoesNotLaunchTerminal()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" },
            Version = new PluginCommand { Name = "Version", Executable = "test", Arguments = "--version" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Version, _projectDir);

        Assert.True(result.Success);
        Assert.Equal("1.2.3", result.Output);
        Assert.Single(_outputRunner.Runs);
        Assert.Empty(_interactiveRunner.Launches);
    }

    [Theory]
    [InlineData(AgentCommandKind.Launch)]
    [InlineData(AgentCommandKind.Resume)]
    [InlineData(AgentCommandKind.Init)]
    [InlineData(AgentCommandKind.Update)]
    public async Task InteractiveCommands_LaunchTerminal(AgentCommandKind kind)
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" },
            Resume = new PluginCommand { Name = "Resume", Executable = "test" },
            Init = new PluginCommand { Name = "Init", Executable = "test" },
            Update = new PluginCommand { Name = "Update", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, kind, _projectDir);

        Assert.True(result.Success);
        Assert.Single(_interactiveRunner.Launches);
        Assert.Empty(_outputRunner.Runs);
    }

    [Fact]
    public async Task MissingCommand_ReturnsFailure()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Version, _projectDir);

        Assert.False(result.Success);
        Assert.Contains("does not support", result.Error);
    }

    [Fact]
    public async Task MissingProjectFolder_ReturnsFailure()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Launch, "/nonexistent");

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Error);
    }
    [Fact]
    public async Task Version_NonZeroExitCode_ReturnsFailure()
    {
        _outputRunner.Result = new ProcessResult
        {
            ExitCode = 1,
            StandardOutput = string.Empty,
            StandardError = "Command not found",
            TimedOut = false
        };

        var plugin = PluginWith(new AgentCommands
        {
            Version = new PluginCommand { Name = "Version", Executable = "test", Arguments = "--version" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Version, _projectDir);

        Assert.False(result.Success);
        Assert.Contains("Command not found", result.Error);
    }
}
