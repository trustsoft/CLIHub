namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
/// Determines whether an agent is installed on the host and whether it is used in a project.
/// </summary>
public interface IAgentDetectionService
{
    /// <summary>
    /// True when any declared system path exists as a file or directory.
    /// </summary>
    bool IsInstalledInSystem(Plugin plugin);

    /// <summary>
    /// True when any declared project indicator exists in the project folder.
    /// </summary>
    bool IsAvailableInProject(Plugin plugin, string projectPath);
}
