namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

public class AgentCommandServiceTests : IDisposable
{
    private readonly string _projectDir;
    private readonly FakeProcessLauncher _launcher = new();
    private readonly AgentCommandService _service;

    public AgentCommandServiceTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "clihub-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDir);
        _service = new AgentCommandService(_launcher, NullLogger<AgentCommandService>.Instance);
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
        Assert.Single(_launcher.Captures);
        Assert.Empty(_launcher.Launches);
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
        Assert.Single(_launcher.Launches);
        Assert.Empty(_launcher.Captures);
        Assert.Equal(_projectDir, _launcher.Launches[0].WorkingDirectory);
    }

    [Fact]
    public async Task MissingCommandKind_Fails()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Resume, _projectDir);

        Assert.False(result.Success);
        Assert.Empty(_launcher.Launches);
        Assert.Empty(_launcher.Captures);
    }

    [Fact]
    public async Task MissingProjectFolder_Fails()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Launch, "Z:\\nope-does-not-exist");

        Assert.False(result.Success);
        Assert.Empty(_launcher.Launches);
    }

    [Fact]
    public async Task VersionCommandFailure_ReportsError()
    {
        _launcher.CaptureResult = new ProcessCaptureResult(false, -1, string.Empty, "boom");
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" },
            Version = new PluginCommand { Name = "Version", Executable = "test" }
        });

        var result = await _service.ExecuteAsync(plugin, AgentCommandKind.Version, _projectDir);

        Assert.False(result.Success);
        Assert.Equal("boom", result.Error);
    }
}
