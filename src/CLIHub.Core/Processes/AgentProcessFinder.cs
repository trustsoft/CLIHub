namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
/// Adapts <see cref="IAgentProcessInspector"/> and <see cref="AgentProcessMatcher"/> 
/// to provide agent process finding.
/// </summary>
public sealed class AgentProcessFinder : IAgentProcessFinder
{
    private readonly IAgentProcessInspector inspector;
    private readonly AgentProcessMatcher matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentProcessFinder"/> class.
    /// </summary>
    /// <param name="inspector">The agent process inspector.</param>
    /// <param name="matcher">The agent process matcher.</param>
    public AgentProcessFinder(
        IAgentProcessInspector inspector,
        AgentProcessMatcher matcher)
    {
        this.inspector = inspector;
        this.matcher = matcher;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentProcess>> FindAsync(
        ProcessMatchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Get candidate process names
        var candidateNames = this.matcher.GetCandidateProcessNames(
            new[] { criteria.Plugin },
            criteria.Detail);

        // Get running processes from inspector (synchronous)
        var instances = this.inspector.GetRunningProcesses(
            candidateNames,
            criteria.Detail);

        // Filter by matcher and map to domain model
        var matches = new List<AgentProcess>();
        foreach (var instance in instances)
        {
            if (this.matcher.IsMatch(instance, criteria.Plugin, criteria.Detail))
            {
                matches.Add(MapToAgentProcess(instance));
            }
        }

        return Task.FromResult<IReadOnlyList<AgentProcess>>(matches);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PluginAgentProcess>> FindAllAsync(
        IReadOnlyList<Plugin> plugins,
        AgentProcessMatchDetail detail,
        CancellationToken cancellationToken = default)
    {
        var matches = new List<PluginAgentProcess>();

        // One candidate-name set and one inspector enumeration cover every plugin,
        // preserving the enumerate-once contract of the catalog-wide query.
        var candidateNames = this.matcher.GetCandidateProcessNames(plugins, detail);
        var instances = this.inspector.GetRunningProcesses(candidateNames, detail);

        foreach (var instance in instances)
        {
            foreach (var plugin in plugins)
            {
                if (this.matcher.IsMatch(instance, plugin, detail))
                {
                    matches.Add(new PluginAgentProcess(plugin, MapToAgentProcess(instance)));
                    break; // One match per process
                }
            }
        }

        return Task.FromResult<IReadOnlyList<PluginAgentProcess>>(matches);
    }

    private static AgentProcess MapToAgentProcess(AgentProcessInstance instance)
    {
        return new AgentProcess
        {
            ProcessId = instance.ProcessId,
            CommandLine = instance.CommandLine ?? string.Empty,
            ExecutablePath = instance.ExecutablePath,
            StartTime = instance.StartTime
        };
    }
}
