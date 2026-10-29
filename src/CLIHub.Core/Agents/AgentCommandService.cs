namespace CLIHub.Core.Agents;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

/// <summary>
///   Executes named agent commands in a project folder.
/// </summary>
public class AgentCommandService : IAgentCommandService
{
    private readonly IProcessOrchestrator orchestrator;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentCommandService> logger;
    private readonly IDomainEventPublisher eventPublisher;

    /// <summary>
    ///   Creates the service.
    /// </summary>
    public AgentCommandService(
        IProcessOrchestrator orchestrator,
        IPreferencesStore preferencesStore,
        ILogger<AgentCommandService> logger,
        IDomainEventPublisher eventPublisher)
    {
        this.orchestrator = orchestrator;
        this.preferencesStore = preferencesStore;
        this.logger = logger;
        this.eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public async Task<AgentCommandResult> ExecuteAsync(Plugin plugin,
                                                       AgentCommandKind kind,
                                                       string projectPath,
                                                       CancellationToken cancellationToken = default)
    {
        var command = plugin.Commands?.Get(kind);
        if (command == null)
        {
            this.logger.LogWarning("{PluginName} does not define a {Kind} command", plugin.Name, kind);
            return new AgentCommandResult(false, null, $"{plugin.Name} does not support the {kind} command");
        }

        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            this.logger.LogWarning("Cannot run {Kind} for {PluginName}: project folder '{ProjectPath}' does not exist",
                kind, plugin.Name, projectPath);
            return new AgentCommandResult(false, null, "Project folder does not exist");
        }

        var runtime = this.preferencesStore.Load().DefaultRuntime;
        var request = LaunchRequest.Create(projectPath, command, runtime, cancellationToken);

        if (kind == AgentCommandKind.Version)
        {
            var result = await this.orchestrator.CaptureOutputAsync(request);
            if (result.IsSuccess && result.ExitCode == 0)
            {
                this.logger.LogInformation("Retrieved version for {PluginName}: {Version}", plugin.Name, result.Output);
                return new AgentCommandResult(true, result.Output, null);
            }
            else
            {
                var error = !string.IsNullOrWhiteSpace(result.Error)
                    ? result.Error
                    : "Failed to get version";
                return new AgentCommandResult(false, result.Output, error);
            }
        }

        return await this.LaunchInteractiveCommandAsync(plugin, projectPath, kind, request);
    }

    private async Task<AgentCommandResult> LaunchInteractiveCommandAsync(
        Plugin plugin,
        string projectPath,
        AgentCommandKind kind,
        LaunchRequest request)
    {
        var launchResult = await this.orchestrator.LaunchInteractiveAsync(request);
        if (launchResult.IsSuccess)
        {
            // Only session-launching kinds publish AgentLaunched; Update is not a launch.
            if (kind is AgentCommandKind.Launch or AgentCommandKind.Resume)
            {
                this.eventPublisher.Publish(new AgentLaunched(
                    plugin.Id,
                    projectPath,
                    kind,
                    DateTimeOffset.UtcNow));
            }

            return new AgentCommandResult(true, null, null);
        }

        return new AgentCommandResult(false, null, launchResult.Error ?? $"Failed to launch {plugin.Name}");
    }
}
