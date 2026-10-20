namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Detects running processes on the system.
/// </summary>
public interface IAgentProcessInspector
{
    /// <summary>
    ///   Enumerates running processes whose process name is in the candidate set, supplying each
    ///   process's identifier, executable path, and start time.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Only processes whose <c>Process.ProcessName</c> is contained in
    ///     <paramref name="candidateProcessNames" /> (compared ordinally, ignoring case) are
    ///     returned; every other process is skipped without reading its executable path or start
    ///     time. Implementations derive no candidates of their own.
    ///   </para>
    ///   <para>
    ///     Assumption A1 (fix-settings-open-delay design): <c>Process.ProcessName</c> equals the
    ///     executable image's file name without extension for every process whose main module is
    ///     accessible, so pre-filtering on the process name cannot drop a process that the
    ///     executable-name matcher (<see cref="AgentProcessMatcher" />) would accept.
    ///   </para>
    /// </remarks>
    /// <param name="candidateProcessNames">
    ///   Candidate process names — the launch executables' file names without extension.
    /// </param>
    /// <returns> List of process instances. Empty if no processes found or access denied. </returns>
    IReadOnlyList<AgentProcessInstance> GetRunningProcesses(IReadOnlySet<string> candidateProcessNames);
}
