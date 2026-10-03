namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
/// Composes the launch-window agent list: agents that are not installed on the host are
/// dropped first, then the optional project-availability filter is applied to the rest.
/// </summary>
public static class AgentListComposer
{
    /// <summary>
    /// Returns the agents that should be listed, each with its project-level availability
    /// (used for dimming). The host-installation gate runs before project detection, so
    /// uninstalled agents are hidden whether or not the project filter is enabled and
    /// whether or not a project is selected.
    /// </summary>
    public static IReadOnlyList<AgentListEntry> Compose(
        IEnumerable<Plugin> plugins,
        IAgentDetectionService detection,
        string? currentProject,
        bool onlyProjectAgents)
    {
        var filterUnavailable = onlyProjectAgents && currentProject != null;
        var entries = new List<AgentListEntry>();

        foreach (var plugin in plugins)
        {
            if (!detection.IsInstalledInSystem(plugin))
            {
                continue;
            }

            var inProject = currentProject != null && detection.IsAvailableInProject(plugin, currentProject);

            if (filterUnavailable && !inProject)
            {
                continue;
            }

            entries.Add(new AgentListEntry(plugin, currentProject == null || inProject));
        }

        return entries;
    }
}

/// <summary>
/// One agent in the composed list: its plugin and whether it is available in the current
/// project (true when no project is selected).
/// </summary>
public readonly record struct AgentListEntry(Plugin Plugin, bool IsAvailable);
