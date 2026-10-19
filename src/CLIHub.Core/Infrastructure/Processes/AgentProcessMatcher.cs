namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
///   Matches running processes to registered agent plugins by the launch executable's file name.
/// </summary>
public sealed class AgentProcessMatcher
{
    private readonly ILogger<AgentProcessMatcher> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AgentProcessMatcher" /> class.
    /// </summary>
    /// <param name="logger"> The logger instance. </param>
    public AgentProcessMatcher(ILogger<AgentProcessMatcher> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    ///   Attempts to match a process instance to a plugin.
    /// </summary>
    /// <remarks>
    ///   Matching compares the process executable's file name with the plugin's declared launch
    ///   executable's file name, ignoring case. A process hosted inside a shared runtime such as
    ///   <c>node.exe</c> or <c>python.exe</c> does not match an agent launched through a command shim.
    /// </remarks>
    /// <param name="processInstance"> The process to match. </param>
    /// <param name="plugin"> The plugin to match against. </param>
    /// <returns> True if the process executable name matches the plugin's launch executable name. </returns>
    public bool IsMatch(AgentProcessInstance processInstance, Plugin plugin)
    {
        if (plugin.Commands?.Launch == null)
        {
            return false;
        }

        var executableName = Path.GetFileNameWithoutExtension(plugin.Commands.Launch.Executable);
        var processName = Path.GetFileNameWithoutExtension(processInstance.ExecutablePath);

        return string.Equals(processName, executableName, StringComparison.OrdinalIgnoreCase);
    }
}
