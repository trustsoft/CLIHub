namespace CLIHub.ViewModels;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

/// <summary>
///   Validates and executes launch-window agent commands, then maps outcomes to presentation effects.
/// </summary>
public sealed class LaunchCommandCoordinator
{
    private const string NoProjectMessage = "Select a project before running an agent command.";
    private const string NoAgentMessage = "Select an agent before running an agent command.";

    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IUserNotificationService _notifications;
    private readonly ILogger<LaunchCommandCoordinator> _logger;

    /// <summary>
    ///   Creates the coordinator over the shared agent workflow and application lifetime.
    /// </summary>
    /// <param name="agentCommandWorkflow"> Workflow executing validated agent commands. </param>
    /// <param name="operationLifetime"> Lifetime tracking asynchronous command operations. </param>
    /// <param name="notifications"> User notifications for command results. </param>
    /// <param name="logger"> Logger for unexpected command failures. </param>
    public LaunchCommandCoordinator(
        IAgentCommandWorkflow agentCommandWorkflow,
        IApplicationOperationLifetime operationLifetime,
        IUserNotificationService notifications,
        ILogger<LaunchCommandCoordinator> logger)
    {
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Runs an agent command for the selected row and project, reporting state through callbacks.
    /// </summary>
    /// <param name="item"> Selected agent row, if any. </param>
    /// <param name="project"> Current project, if any. </param>
    /// <param name="kind"> Command kind to execute. </param>
    /// <param name="reportStatus"> Callback receiving binding-facing status text. </param>
    /// <param name="refreshAgents"> Callback refreshing the agent pane after execution. </param>
    /// <returns> A task that completes when the tracked command operation is observed. </returns>
    public Task RunAsync(
        AgentItem? item,
        Project? project,
        AgentCommandKind kind,
        Action<string> reportStatus,
        Action refreshAgents)
    {
        ArgumentNullException.ThrowIfNull(reportStatus);
        ArgumentNullException.ThrowIfNull(refreshAgents);

        var operationName = $"{kind} {item?.Name ?? "agent command"}";
        return _operationLifetime.RunAsync(
            operationName,
            cancellationToken => AsyncOperationRunner.RunAsync(
                operationName,
                () => ExecuteAsync(item, project, kind, reportStatus, refreshAgents, cancellationToken),
                _logger,
                reportStatus,
                cancellationToken));
    }

    private async Task ExecuteAsync(
        AgentItem? item,
        Project? project,
        AgentCommandKind kind,
        Action<string> reportStatus,
        Action refreshAgents,
        CancellationToken cancellationToken)
    {
        if (item is null)
        {
            reportStatus(NoAgentMessage);
            return;
        }

        if (project is null)
        {
            reportStatus(NoProjectMessage);
            return;
        }

        if (item.Plugin.Commands?.Get(kind) is null)
        {
            reportStatus($"{item.Name} does not support the {kind.ToString().ToLowerInvariant()} command.");
            return;
        }

        reportStatus($"{kind} {item.Name}...");

        var result = await _agentCommandWorkflow.ExecuteAsync(item.Plugin, project, kind, cancellationToken);
        var error = result.Error ?? "no error details";

        if (kind == AgentCommandKind.Version)
        {
            if (result.Success)
            {
                var version = result.Output ?? string.Empty;
                reportStatus($"{item.Name} version: {result.Output}");
                _notifications.ShowInformation(version, $"{item.Name} version");
            }
            else
            {
                reportStatus($"{item.Name} version failed: {error}");
            }
        }
        else
        {
            reportStatus(result.Success
                ? $"{kind} started for {item.Name}"
                : $"{kind} failed: {error}");
        }

        refreshAgents();
    }
}
