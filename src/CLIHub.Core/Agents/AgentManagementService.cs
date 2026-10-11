namespace CLIHub.Core.Agents;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Services;

/// <summary>
///   On-demand agent operations for the management surface, composing detection, version, command,
///   and running-agent services.
/// </summary>
public sealed class AgentManagementService : IAgentManagementService
{
    private readonly IAgentDetectionService _detection;
    private readonly IAgentVersionService _version;
    private readonly IAgentCommandService _commands;
    private readonly AgentProcessMonitor _processMonitor;
    private readonly ILogger<AgentManagementService> _logger;

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="commands"> Agent command execution. </param>
    /// <param name="processMonitor"> Running-agent monitor used to block updates. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementService(
        IAgentDetectionService detection,
        IAgentVersionService version,
        IAgentCommandService commands,
        AgentProcessMonitor processMonitor,
        ILogger<AgentManagementService> logger)
    {
        _detection = detection ?? throw new ArgumentNullException(nameof(detection));
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        _detection.Invalidate();
        _version.Invalidate();

        var installed = _detection.IsInstalledInSystem(plugin);
        var version = await _version.GetVersionAsync(plugin, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Probed {PluginId}: installed={Installed}, version={Version}", plugin.Id, installed, version);
        return new AgentProbeResult(installed, version);
    }

    /// <inheritdoc />
    public async Task<AgentUpdateResult> UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.Commands?.Get(AgentCommandKind.Update) is null)
        {
            return new AgentUpdateResult(
                AgentUpdateOutcome.Unsupported,
                $"{plugin.Name} does not support the update command");
        }

        var runningAgents = _processMonitor.GetRunningAgents();
        if (runningAgents.Count > 0)
        {
            var names = string.Join(", ", runningAgents.Select(agent => agent.Plugin.Name));
            return new AgentUpdateResult(
                AgentUpdateOutcome.Blocked,
                $"Update blocked while an agent is running: {names}");
        }

        var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = await _commands
            .ExecuteAsync(plugin, AgentCommandKind.Update, workingDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (result.Success)
        {
            return new AgentUpdateResult(AgentUpdateOutcome.Started, null);
        }

        return new AgentUpdateResult(
            AgentUpdateOutcome.Failed,
            result.Error ?? $"Failed to start the {plugin.Name} update");
    }
}
