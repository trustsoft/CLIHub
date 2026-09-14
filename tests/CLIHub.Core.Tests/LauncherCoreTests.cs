using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class LauncherCoreTests
{
    private const string Project = "C:\\projects\\demo";

    private static AgentManifest RunAgent(string? runtime = null) => new()
    {
        Id = "claude",
        Name = "Claude Code",
        Actions = new Dictionary<string, AgentActionSpec>
        {
            ["run"] = new() { Command = "claude", Runtime = runtime }
        }
    };

    [Fact]
    public void Start_DefaultRuntime_LaunchesCmdInProjectDirectory()
    {
        var runner = new CapturingProcessRunner();
        var launcher = new LauncherCore(runner);

        var result = launcher.Start(RunAgent(), "run", configRuntime: null, Project);

        Assert.True(result.Success);
        Assert.Equal("cmd", result.Runtime);
        var started = Assert.Single(runner.Started);
        Assert.Equal("cmd.exe", started.FileName);
        Assert.Equal("/k \"claude\"", started.Arguments);
        Assert.Equal(Project, started.WorkingDirectory);
    }

    [Fact]
    public void Start_WtMissing_FallsBackWithWarning()
    {
        var runner = new CapturingProcessRunner { CommandExistsHandler = _ => false };
        var launcher = new LauncherCore(runner);

        var result = launcher.Start(RunAgent(runtime: "wt"), "run", configRuntime: null, Project);

        Assert.True(result.Success);
        Assert.Equal(RuntimeResolver.DefaultRuntime, result.Runtime);
        Assert.NotNull(result.Warning);
        var started = Assert.Single(runner.Started);
        Assert.Equal("cmd.exe", started.FileName);
    }

    [Fact]
    public void Start_WtPresent_UsesWindowsTerminal()
    {
        var runner = new CapturingProcessRunner { CommandExistsHandler = _ => true };
        var launcher = new LauncherCore(runner);

        var result = launcher.Start(RunAgent(runtime: "wt"), "run", configRuntime: null, Project);

        Assert.True(result.Success);
        Assert.Equal("wt", result.Runtime);
        Assert.Null(result.Warning);
        Assert.Equal("wt.exe", runner.Started.Single().FileName);
    }

    [Fact]
    public void Start_MissingAction_ReturnsFailureWithoutLaunching()
    {
        var runner = new CapturingProcessRunner();
        var launcher = new LauncherCore(runner);

        var result = launcher.Start(RunAgent(), "resume", configRuntime: null, Project);

        Assert.False(result.Success);
        Assert.Empty(runner.Started);
    }
}
