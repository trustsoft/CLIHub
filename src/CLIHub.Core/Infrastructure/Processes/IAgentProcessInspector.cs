namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Detects running processes on the system.
/// </summary>
public interface IAgentProcessInspector
{
    /// <summary>
    ///   Enumerates running processes whose process name is in the candidate set, supplying each
    ///   process's identifier, executable path, and start time — plus, under
    ///   <see cref="AgentProcessMatchDetail.CommandLine" /> detail, each candidate process's
    ///   command line when it can be read.
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
    ///   <para>
    ///     Under <see cref="AgentProcessMatchDetail.ExecutableName" /> the caller needs executable
    ///     names only and no command-line retrieval occurs. Under
    ///     <see cref="AgentProcessMatchDetail.CommandLine" /> the implementation additionally
    ///     retrieves candidate processes' command lines, but a command line may still be null per
    ///     process — retrieval is best effort, and an unreadable, timed-out, or degraded
    ///     enumeration supplies no command line for the affected processes (matching then
    ///     evaluates by executable name only).
    ///   </para>
    /// </remarks>
    /// <param name="candidateProcessNames">
    ///   Candidate process names — the launch executables' file names without extension, plus the
    ///   runtime host names exactly when command-line matching is active.
    /// </param>
    /// <param name="detail"> The matching fidelity the caller will evaluate. </param>
    /// <returns> List of process instances. Empty if no processes found or access denied. </returns>
    IReadOnlyList<AgentProcessInstance> GetRunningProcesses(
        IReadOnlySet<string> candidateProcessNames,
        AgentProcessMatchDetail detail);
}
