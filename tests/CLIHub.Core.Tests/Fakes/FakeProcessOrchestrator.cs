namespace CLIHub.Core.Tests.Fakes;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

using ProcessResult = CLIHub.Core.Processes.ProcessResult;

/// <summary>
///   Test fake for <see cref="IProcessOrchestrator"/> that returns default results.
/// </summary>
public sealed class FakeProcessOrchestrator : IProcessOrchestrator
{
    /// <inheritdoc/>
    public Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request) 
        => Task.FromResult(ProcessResult.Success(0));

    /// <inheritdoc/>
    public Task<ProcessResult> CaptureOutputAsync(LaunchRequest request) 
        => Task.FromResult(ProcessResult.Success(0));

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria)
        => Task.FromResult<IReadOnlyList<AgentProcess>>(new List<AgentProcess>());

    /// <inheritdoc/>
    public Task<IReadOnlyList<PluginAgentProcess>> FindRunningAgentsAsync(
        IReadOnlyList<Plugin> plugins,
        AgentProcessMatchDetail detail)
        => Task.FromResult<IReadOnlyList<PluginAgentProcess>>(new List<PluginAgentProcess>());

}
