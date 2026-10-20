namespace CLIHub.Core.Agents;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

/// <summary>
///   Monitors running agent processes for update safety checks.
/// </summary>
public sealed class AgentProcessMonitor
{
    private readonly IAgentProcessInspector processInspector;
    private readonly AgentProcessMatcher processMatcher;
    private readonly IPluginCatalog pluginCatalog;
    private readonly ILogger<AgentProcessMonitor> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AgentProcessMonitor" /> class.
    /// </summary>
    /// <param name="processInspector"> The process inspector. </param>
    /// <param name="processMatcher"> The process matcher. </param>
    /// <param name="pluginCatalog"> The plugin catalog. </param>
    /// <param name="logger"> The logger instance. </param>
    public AgentProcessMonitor(IAgentProcessInspector processInspector,
                               AgentProcessMatcher processMatcher,
                               IPluginCatalog pluginCatalog,
                               ILogger<AgentProcessMonitor> logger)
    {
        this.processInspector = processInspector;
        this.processMatcher = processMatcher;
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
    /// <returns> True if at least one process matching the agent is running. </returns>
    public bool HasRunningAgents(Plugin plugin)
    {
        return this.GetRunningAgents(plugin).Count > 0;
    }

    /// <summary>
    ///   Gets the list of currently running agents with their process details.
    /// </summary>
    /// <remarks>
    ///   Process enumeration is pre-filtered to the candidate names derived from the catalog's
    ///   launch executables (see <see cref="AgentProcessMatcher.GetCandidateProcessNames" />);
    ///   processes whose name cannot match any launch executable are never inspected.
    /// </remarks>
    /// <returns> List of running agents. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents()
    {
        var runningAgents = new List<RunningAgent>();

        try
        {
            var plugins = this.pluginCatalog.GetAllPlugins();
            var candidateProcessNames = this.processMatcher.GetCandidateProcessNames(plugins);
            var processes = this.processInspector.GetRunningProcesses(candidateProcessNames);

            foreach (var process in processes)
            {
                foreach (var plugin in plugins)
                {
                    if (this.processMatcher.IsMatch(process, plugin))
                    {
                        runningAgents.Add(new RunningAgent(plugin, process));
                        break; // One match per process
                    }
                }
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
    /// <returns> List of running agents matching the agent. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        return this.GetRunningAgents()
            .Where(runningAgent => runningAgent.Plugin.Id == plugin.Id)
            .ToList();
    }
}
