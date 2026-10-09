namespace CLIHub.Core.Models;

/// <summary>
///   A snapshot of a running process that may belong to an agent.
/// </summary>
/// <param name="ProcessId"> The operating system process identifier. </param>
/// <param name="ExecutablePath"> The full path to the process executable. </param>
/// <param name="CommandLine"> The command line used to start the process, including arguments. </param>
/// <param name="StartTime"> When the process was started. </param>
public sealed record AgentProcessInstance(
    int ProcessId,
    string ExecutablePath,
    string CommandLine,
    DateTime StartTime
);
