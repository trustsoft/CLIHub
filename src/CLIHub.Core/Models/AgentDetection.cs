namespace CLIHub.Core.Models;

/// <summary>
/// Folder/file markers used to detect whether an agent is installed on the host and
/// whether it is used within a project.
/// </summary>
public class AgentDetection
{
    /// <summary>
    /// Paths (may contain environment variables) whose existence indicates the agent
    /// is installed on the host.
    /// </summary>
    public List<string> SystemPaths { get; set; } = new();

    /// <summary>
    /// Folder or file names, relative to a project root, whose existence indicates the
    /// agent is used in that project.
    /// </summary>
    public List<string> ProjectIndicators { get; set; } = new();
}
