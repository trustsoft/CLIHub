namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
///   Matches running processes to registered agent plugins.
/// </summary>
public sealed class AgentProcessMatcher
{
    private readonly ILogger<AgentProcessMatcher> _logger;

    public AgentProcessMatcher(ILogger<AgentProcessMatcher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///   Attempts to match a process instance to a plugin.
    /// </summary>
    /// <param name="processInstance"> The process to match. </param>
    /// <param name="plugin"> The plugin to match against. </param>
    /// <returns> True if the process belongs to this plugin. </returns>
    public bool IsMatch(AgentProcessInstance processInstance, Plugin plugin)
    {
        if (plugin.Commands?.Launch == null)
        {
            return false;
        }

        var launchCommand = plugin.Commands.Launch;
        var executableName = Path.GetFileNameWithoutExtension(launchCommand.Executable);
        var processName = Path.GetFileNameWithoutExtension(processInstance.ExecutablePath);

        // Direct executable name match (e.g., "opencode.exe" matches "opencode")
        if (string.Equals(processName, executableName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if process is a runtime (node, python, etc.) running the agent script
        if (IsRuntimeProcess(processInstance, plugin))
        {
            return true;
        }

        return false;
    }

    private bool IsRuntimeProcess(AgentProcessInstance processInstance, Plugin plugin)
    {
        var processName = Path.GetFileNameWithoutExtension(processInstance.ExecutablePath).ToLowerInvariant();
        
        // Common runtimes
        var isNodeRuntime = processName == "node" || processName == "nodejs";
        var isPythonRuntime = processName == "python" || processName == "python3";
        
        if (!isNodeRuntime && !isPythonRuntime)
        {
            return false;
        }

        // Check if command line contains agent-specific markers
        if (string.IsNullOrEmpty(processInstance.CommandLine))
        {
            return false;
        }

        var commandLine = processInstance.CommandLine.ToLowerInvariant();
        
        // Check for plugin ID or executable name in command line
        if (commandLine.Contains(plugin.Id.ToLowerInvariant()))
        {
            return true;
        }

        var executableName = Path.GetFileNameWithoutExtension(plugin.Commands?.Launch?.Executable ?? string.Empty);
        if (!string.IsNullOrEmpty(executableName) && 
            commandLine.Contains(executableName.ToLowerInvariant()))
        {
            return true;
        }

        // Check for known paths from SystemPaths
        if (plugin.Detection?.SystemPaths != null)
        {
            foreach (var systemPath in plugin.Detection.SystemPaths)
            {
                var normalizedPath = systemPath.Replace('/', '\\').ToLowerInvariant();
                if (commandLine.Contains(normalizedPath))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
