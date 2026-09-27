using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace CLIHub.Core.Services;

/// <summary>
/// Executes named agent commands in a project folder.
/// </summary>
public class AgentCommandService : IAgentCommandService
{
    private readonly IProcessLauncher _processLauncher;
    private readonly ILogger<AgentCommandService> _logger;

    public AgentCommandService(IProcessLauncher processLauncher, ILogger<AgentCommandService> logger)
    {
        _processLauncher = processLauncher;
        _logger = logger;
    }

    public async Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        AgentCommandKind kind,
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var command = plugin.Commands?.Get(kind);
        if (command == null)
        {
            _logger.LogWarning("{PluginName} does not define a {Kind} command", plugin.Name, kind);
            return new AgentCommandResult(false, null, $"{plugin.Name} does not support the {kind} command");
        }

        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            _logger.LogWarning("Cannot run {Kind} for {PluginName}: project folder '{ProjectPath}' does not exist",
                kind, plugin.Name, projectPath);
            return new AgentCommandResult(false, null, "Project folder does not exist");
        }

        if (kind == AgentCommandKind.Version)
            return await RunVersionAsync(plugin, command, projectPath, cancellationToken);

        var started = _processLauncher.LaunchProcess(command, projectPath);
        return started
            ? new AgentCommandResult(true, null, null)
            : new AgentCommandResult(false, null, $"Failed to launch {plugin.Name}");
    }

    private async Task<AgentCommandResult> RunVersionAsync(
        Plugin plugin,
        PluginCommand command,
        string projectPath,
        CancellationToken cancellationToken)
    {
        var result = await _processLauncher.CaptureOutputAsync(
            command.Executable, command.Arguments, projectPath, cancellationToken);

        if (!result.Started || result.ExitCode != 0)
        {
            var error = !string.IsNullOrWhiteSpace(result.StdErr)
                ? result.StdErr
                : $"Failed to get version for {plugin.Name}";
            return new AgentCommandResult(false, result.StdOut, error);
        }

        _logger.LogInformation("Retrieved version for {PluginName}: {Version}", plugin.Name, result.StdOut);
        return new AgentCommandResult(true, result.StdOut, null);
    }
}
