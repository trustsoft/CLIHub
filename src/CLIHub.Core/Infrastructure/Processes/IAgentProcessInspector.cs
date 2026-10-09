namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Detects running processes on the system.
/// </summary>
public interface IAgentProcessInspector
{
    /// <summary>
    ///   Enumerates all running processes accessible to the current user.
    /// </summary>
    /// <returns> List of process instances. Empty if no processes found or access denied. </returns>
    IReadOnlyList<AgentProcessInstance> GetRunningProcesses();
}
