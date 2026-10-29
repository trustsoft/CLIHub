namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

/// <summary>
/// Adapts <see cref="IAgentProcessInspector"/> and <see cref="AgentProcessMatcher"/> 
/// to provide agent process finding.
/// </summary>
internal sealed class AgentProcessFinder : IAgentProcessFinder
{
    private readonly IAgentProcessInspector inspector;
    private readonly AgentProcessMatcher matcher;
    private readonly IPluginCatalog catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentProcessFinder"/> class.
    /// </summary>
    /// <param name="inspector">The agent process inspector.</param>
    /// <param name="matcher">The agent process matcher.</param>
    /// <param name="catalog">The plugin catalog.</param>
    public AgentProcessFinder(
        IAgentProcessInspector inspector,
        AgentProcessMatcher matcher,
        IPluginCatalog catalog)
    {
        this.inspector = inspector;
        this.matcher = matcher;
        this.catalog = catalog;
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

    private static AgentProcess MapToAgentProcess(AgentProcessInstance instance)
    {
        // Runtime is null for runtime-hosted processes (Node.js, Python)
        // as RuntimeKind enum doesn't have corresponding values
        RuntimeExecutable? runtime = null;

        var executableName = Path.GetFileNameWithoutExtension(instance.ExecutablePath);
        var isRuntimeHosted = AgentProcessMatcher.RuntimeHostProcessNames.Contains(executableName);

        // For now, runtime-hosted processes have null Runtime
        // Shell-hosted processes also have null Runtime in this phase
        // (Phase 3 consumer migration will populate this properly)

        return new AgentProcess
        {
            ProcessId = instance.ProcessId,
            CommandLine = instance.CommandLine ?? string.Empty,
            Runtime = runtime,
            StartTime = instance.StartTime
        };
    }
}
