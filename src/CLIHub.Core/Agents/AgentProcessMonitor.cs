namespace CLIHub.Core.Agents;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;

/// <summary>
///   Monitors running agent processes for update safety checks.
/// </summary>
public sealed class AgentProcessMonitor
{
    private readonly IProcessOrchestrator orchestrator;
    private readonly IPluginCatalog pluginCatalog;
    private readonly ILogger<AgentProcessMonitor> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AgentProcessMonitor" /> class.
    /// </summary>
    /// <param name="orchestrator"> The process orchestrator. </param>
    /// <param name="pluginCatalog"> The plugin catalog. </param>
    /// <param name="logger"> The logger instance. </param>
    public AgentProcessMonitor(
        IProcessOrchestrator orchestrator,
        IPluginCatalog pluginCatalog,
        ILogger<AgentProcessMonitor> logger)
    {
        this.orchestrator = orchestrator;
        this.pluginCatalog = pluginCatalog;
        this.logger = logger;
    }

    /// <summary>
    ///   Checks if any registered agents are currently running.
    /// </summary>
    /// <returns> True if at least one agent process is running. </returns>
    public bool HasRunningAgents()
    {
        return this.GetRunningAgents().Count > 0;
    }

    /// <summary>
    ///   Checks if processes matching the given agent are currently running.
    /// </summary>
    /// <param name="plugin"> The agent whose processes are checked. </param>
    /// <param name="detail">
    ///   The matching fidelity: <see cref="AgentProcessMatchDetail.ExecutableName" /> consults
    ///   executable names only; <see cref="AgentProcessMatchDetail.CommandLine" /> additionally
    ///   matches runtime-hosted agents by command line. Update gating uses
    ///   <see cref="AgentProcessMatchDetail.CommandLine" />.
    /// </param>
    /// <returns> True if at least one process matching the agent is running. </returns>
    public bool HasRunningAgents(Plugin plugin, AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    {
        return this.GetRunningAgents(plugin, detail).Count > 0;
    }

    /// <summary>
    ///   Gets the list of currently running agents with their process details.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Process enumeration is pre-filtered to the candidate names derived from the catalog's
    ///     launch executables (see <see cref="AgentProcessMatcher.GetCandidateProcessNames" />);
    ///     processes whose name cannot match any launch executable are never inspected. Under
    ///     <see cref="AgentProcessMatchDetail.CommandLine" /> the runtime host names join the
    ///     candidates and command lines are retrieved for candidate processes; under
    ///     <see cref="AgentProcessMatchDetail.ExecutableName" /> no command lines are retrieved.
    ///     Update gating uses <see cref="AgentProcessMatchDetail.CommandLine" />.
    ///   </para>
    /// </remarks>
    /// <param name="detail"> The matching fidelity of the enumeration. </param>
    /// <returns> List of running agents. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents(
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    {
        var runningAgents = new List<RunningAgent>();

        try
        {
            var plugins = this.pluginCatalog.GetAllPlugins();
            var matches = this.orchestrator.FindRunningAgentsAsync(plugins, detail)
                .GetAwaiter().GetResult();

            foreach (var match in matches)
            {
                runningAgents.Add(new RunningAgent(match.Plugin, MapToInstance(match.Process)));
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to check for running agents");
        }

        return runningAgents;
    }

    /// <summary>
    ///   Gets the list of running processes matching the given agent.
    /// </summary>
    /// <param name="plugin"> The agent whose processes are returned. </param>
    /// <param name="detail">
    ///   The matching fidelity: <see cref="AgentProcessMatchDetail.ExecutableName" /> consults
    ///   executable names only; <see cref="AgentProcessMatchDetail.CommandLine" /> additionally
    ///   matches runtime-hosted agents by command line. Update gating uses
    ///   <see cref="AgentProcessMatchDetail.CommandLine" />.
    /// </param>
    /// <returns> List of running agents matching the agent. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents(
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        try
        {
            var criteria = ProcessMatchCriteria.Create(plugin, detail);
            var processes = this.orchestrator.FindRunningAgentsAsync(criteria)
                .GetAwaiter().GetResult();

            return processes
                .Select(p => new RunningAgent(plugin, MapToInstance(p)))
                .ToList();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to check for running agents for plugin {PluginId}", plugin.Id);
            return Array.Empty<RunningAgent>();
        }
    }

    private static AgentProcessInstance MapToInstance(AgentProcess process)
        => new(
            ProcessId: process.ProcessId,
            ExecutablePath: process.ExecutablePath,
            StartTime: process.StartTime,
            CommandLine: process.CommandLine
        );
}
