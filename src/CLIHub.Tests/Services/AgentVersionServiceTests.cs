using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CLIHub.Tests.Services;

public class AgentVersionServiceTests
{
    private readonly FakeProcessLauncher _launcher = new();

    private AgentVersionService CreateService() =>
        new(_launcher, NullLogger<AgentVersionService>.Instance);

    private static Plugin PluginWithVersion(bool includeVersion)
    {
        var commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = "agent" }
        };
        if (includeVersion)
            commands.Version = new PluginCommand { Executable = "agent", Arguments = "--version" };

        return new Plugin { Id = "agent", Name = "Agent", Commands = commands };
    }

    [Fact]
    public async Task GetVersion_CommandDefined_ReturnsFirstLine()
    {
        _launcher.CaptureResult = new ProcessCaptureResult(true, 0, "1.2.3\nsome banner", string.Empty);
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("1.2.3", version);
        Assert.Single(_launcher.Captures);
    }

    [Theory]
    [InlineData("GitHub Copilot CLI 1.0.88.", "1.0.88")]
    [InlineData("0.30.0 (OpenClaude)", "0.30.0")]
    [InlineData("1.18.32", "1.18.32")]
    [InlineData("v2.5", "2.5")]
    public async Task GetVersion_ExtractsVersionNumber(string output, string expected)
    {
        _launcher.CaptureResult = new ProcessCaptureResult(true, 0, output, string.Empty);
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal(expected, version);
    }

    [Fact]
    public async Task GetVersion_NoVersionNumber_ReturnsLineAsIs()
    {
        _launcher.CaptureResult = new ProcessCaptureResult(true, 0, "no digits here", string.Empty);
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
        Assert.Empty(_launcher.Captures);
    }

    [Fact]
    public async Task GetVersion_CommandFails_ReturnsNull()
    {
        _launcher.CaptureResult = new ProcessCaptureResult(false, -1, string.Empty, "boom");
        var service = CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_IsCached()
    {
        var service = CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        await service.GetVersionAsync(plugin);

        Assert.Single(_launcher.Captures);
    }

    [Fact]
    public async Task Invalidate_ForcesReRun()
    {
        var service = CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        service.Invalidate();
        await service.GetVersionAsync(plugin);

        Assert.Equal(2, _launcher.Captures.Count);
    }
}
