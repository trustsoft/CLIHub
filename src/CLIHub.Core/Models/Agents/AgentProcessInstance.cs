namespace CLIHub.Core.Models;

/// <summary>
///   A snapshot of a running process that may belong to an agent.
/// </summary>
/// <param name="ProcessId"> The operating system process identifier. </param>
/// <param name="ExecutablePath"> The full path to the process executable. </param>
/// <param name="StartTime"> When the process was started. </param>
/// <param name="CommandLine">
///   The process's full command line, supplied only for command-line-detail inspection
///   (<c>AgentProcessMatchDetail.CommandLine</c>). It is null when command lines are not retrieved
///   (executable-name detail) or could not be read for this process (retrieval degraded).
/// </param>
public sealed record AgentProcessInstance(int ProcessId,
                                          string ExecutablePath,
                                          DateTime StartTime,
                                          string? CommandLine = null
);
