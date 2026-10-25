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
    private readonly IAgentUpdateAvailabilityService availability;
    private readonly IAgentCommandService commands;
    private readonly AgentProcessMonitor processMonitor;
    private readonly ILogger<AgentManagementService> logger;

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="availability"> Update availability service. </param>
    /// <param name="commands"> Agent command execution. </param>
    /// <param name="processMonitor"> Running-agent monitor used to block updates. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementService(IAgentDetectionService detection,
                                  IAgentVersionService version,
                                  IAgentUpdateAvailabilityService availability,
                                  IAgentCommandService commands,
                                  AgentProcessMonitor processMonitor,
                                  ILogger<AgentManagementService> logger)
    {
        this.detection = detection ?? throw new ArgumentNullException(nameof(detection));
        this.version = version ?? throw new ArgumentNullException(nameof(version));
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
        this.processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentProbeResult> ProbeAsync(Plugin plugin,
                                                   CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        // Invalidate only the probed agent so a detect-all pass keeps every earlier card's
        // fresh results cached instead of clearing them on the next iteration.
        this.detection.InvalidateFor(plugin);
        this.version.InvalidateFor(plugin);
        this.availability.InvalidateFor(plugin);

        var installed = this.detection.IsInstalledInSystem(plugin);

        // A missing CLI has no version to retrieve; running the version command would be a
        // doomed process launch, so the probe settles on the detection result alone.
        if (!installed)
        {
            this.logger.LogInformation("Probed {PluginId}: installed={Installed}, version={Version}", plugin.Id, installed, null);
            return new AgentProbeResult(false, null);
        }

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

        // The full command-line-detail enumeration runs off the caller's context so the process
        // snapshot (and the command-line retrieval under it) never lands on a UI thread; the
        // blocked outcome and message shape are unchanged.
        var runningAgents = await Task.Run(
            () => this.processMonitor.GetRunningAgents(plugin),
            cancellationToken).ConfigureAwait(false);
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
            // The interactive updater may still be running; invalidation only forces the next
            // lookups to re-probe instead of serving the pre-update results.
            this.version.InvalidateFor(plugin);
            this.availability.InvalidateFor(plugin);
            return new AgentUpdateResult(AgentUpdateOutcome.Started, null);
        }

        return new AgentUpdateResult(
            AgentUpdateOutcome.Failed,
            result.Error ?? $"Failed to start the {plugin.Name} update");
    }
}
