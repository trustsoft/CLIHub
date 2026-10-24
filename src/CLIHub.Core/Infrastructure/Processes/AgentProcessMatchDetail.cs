namespace CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   The fidelity of running-agent process matching.
/// </summary>
public enum AgentProcessMatchDetail
{
    /// <summary>
    ///   Matches the process executable's file name against the launch executable only. Cheap:
    ///   no command-line retrieval.
    /// </summary>
    ExecutableName,

    /// <summary>
    ///   Additionally matches processes hosted inside a shared runtime (for example
    ///   <c>node.exe</c>) by their command line. Authoritative for update gating.
    /// </summary>
    CommandLine
}
