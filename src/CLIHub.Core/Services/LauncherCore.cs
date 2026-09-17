using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class LauncherCore
{
    private readonly IProcessRunner _processRunner;
    private readonly RuntimeResolver _runtimeResolver;

    public LauncherCore(IProcessRunner processRunner, RuntimeResolver? runtimeResolver = null)
    {
        _processRunner = processRunner;
        _runtimeResolver = runtimeResolver ?? new RuntimeResolver();
    }

    public LaunchResult Start(AgentManifest agent, string actionKey, string? configRuntime, string projectPath)
    {
        if (agent.Actions is null ||
            !agent.Actions.TryGetValue(actionKey, out var action) ||
            string.IsNullOrWhiteSpace(action.Command))
        {
            return LaunchResult.Failed($"Action '{actionKey}' is not available for agent '{agent.Id}'.");
        }

        var runtime = _runtimeResolver.Resolve(action, configRuntime);
        string? warning = null;

        if (runtime == "wt" && !_processRunner.CommandExists("wt.exe"))
        {
            runtime = RuntimeResolver.DefaultRuntime;
            warning = "Windows Terminal (wt) not found — launching via the fallback runtime.";
        }

        var command = CommandBuilder.Build(runtime, action.Command!, projectPath);
        _processRunner.StartDetached(command.FileName, command.Arguments, command.WorkingDirectory);

        return LaunchResult.Ok(runtime, warning);
    }
}
