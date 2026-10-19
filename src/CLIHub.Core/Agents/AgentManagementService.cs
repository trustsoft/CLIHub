namespace CLIHub.Core.Agents;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

/// <summary>
///   On-demand agent operations for the management surface,
///   composing detection, version, command, and running-agent services.
/// </summary>
public sealed class AgentManagementService : IAgentManagementService
{
    private readonly IAgentDetectionService detection;
    private readonly IAgentVersionService version;
    private readonly IAgentCommandService commands;
    private readonly AgentProcessMonitor processMonitor;
    private readonly ILogger<AgentManagementService> logger;

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="commands"> Agent command execution. </param>
    /// <param name="processMonitor"> Running-agent monitor used to block updates. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementService(IAgentDetectionService detection,
                                  IAgentVersionService version,
                                  IAgentCommandService commands,
                                  AgentProcessMonitor processMonitor,
                                  ILogger<AgentManagementService> logger)
    {
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.version = version ?? throw new ArgumentNullException(nameof(version));
        this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
        this.processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentProbeResult> ProbeAsync(Plugin plugin,
                                                   CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        this.detection.Invalidate();
        this.version.Invalidate();

        var installed = this.detection.IsInstalledInSystem(plugin);
        var version = await this.version.GetVersionAsync(plugin, cancellationToken).ConfigureAwait(false);

        this.logger.LogInformation("Probed {PluginId}: installed={Installed}, version={Version}", plugin.Id, installed, version);
        return new AgentProbeResult(installed, version);
    }

    /// <inheritdoc />
    public async Task<AgentUpdateResult> UpdateAsync(Plugin plugin,
                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.Commands?.Get(AgentCommandKind.Update) is null)
        {
            return new AgentUpdateResult(
                AgentUpdateOutcome.Unsupported,
                $"{plugin.Name} does not support the update command");
        }

        var runningAgents = this.processMonitor.GetRunningAgents(plugin);
        if (runningAgents.Count > 0)
        {
            var names = string.Join(", ", runningAgents.Select(agent => agent.Plugin.Name));
            return new AgentUpdateResult(
                AgentUpdateOutcome.Blocked,
                $"Update blocked while the agent is running: {names}");
        }

        var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = await this.commands
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
