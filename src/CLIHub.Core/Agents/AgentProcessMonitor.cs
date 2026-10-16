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
    public AgentProcessMonitor(
        IAgentProcessInspector processInspector,
        Infrastructure.Processes.AgentProcessMatcher processMatcher,
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
    ///   Gets the list of currently running agents with their process details.
    /// </summary>
    /// <returns> List of running agents. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents()
    {
        var runningAgents = new List<RunningAgent>();

        try
        {
            var processes = this.processInspector.GetRunningProcesses();
            var plugins = this.pluginCatalog.GetAllPlugins();

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
}
