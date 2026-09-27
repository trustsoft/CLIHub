using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

/// <summary>
/// Detects agent availability using only file-system checks.
/// </summary>
public class AgentDetectionService : IAgentDetectionService
{
    public bool IsInstalledInSystem(Plugin plugin)
    {
        foreach (var path in plugin.Detection?.SystemPaths ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var expanded = Environment.ExpandEnvironmentVariables(path);
            if (File.Exists(expanded) || Directory.Exists(expanded))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsAvailableInProject(Plugin plugin, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return false;
        }

        foreach (var indicator in plugin.Detection?.ProjectIndicators ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(indicator))
            {
                continue;
            }

            var combined = Path.Combine(projectPath, indicator);
            if (File.Exists(combined) || Directory.Exists(combined))
            {
                return true;
            }
        }

        return false;
    }
}
