namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Determines whether an agent is installed on the host and whether it is used in a project.
/// </summary>
public interface IAgentDetectionService
{
    /// <summary>
    ///   True when any declared system path exists as a file or directory.
    /// </summary>
    bool IsInstalledInSystem(Plugin plugin);

    /// <summary>
    ///   True when any declared project indicator exists in the project folder.
    /// </summary>
    bool IsAvailableInProject(Plugin plugin, string projectPath);

    /// <summary>
    ///   Clears cached detection results so the next detection re-checks the file system.
    /// </summary>
    void Invalidate();

    /// <summary>
    ///   Clears the cached system and project detection results of one agent so its next
    ///   detection re-checks the file system. Other agents' cached results are preserved.
    /// </summary>
    /// <param name="plugin"> The agent whose results are cleared. </param>
    void InvalidateFor(Plugin plugin);
}
