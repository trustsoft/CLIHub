namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Matches running processes to registered agent plugins by the launch executable's file name.
/// </summary>
public sealed class AgentProcessMatcher
{
    /// <summary>
    ///   Derives the candidate process names for the given plugins: the launch executable's file
    ///   name without extension, which is exactly the comparison basis <see cref="IsMatch" /> uses.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The process inspector port pre-filters processes by this set before reading expensive
    ///     per-process details such as the main module. A process can match a plugin only when its
    ///     executable file name equals one of these candidates, so the filter cannot drop a match
    ///     the matcher would make.
    ///   </para>
    /// </remarks>
    /// <param name="plugins"> The plugins whose launch executables define the candidates. </param>
    /// <returns>
    ///   Candidate process names compared ordinally, ignoring case. Plugins without a launch
    ///   command contribute nothing.
    /// </returns>
    public IReadOnlySet<string> GetCandidateProcessNames(IEnumerable<Plugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            if (plugin.Commands?.Launch is null)
            {
                continue;
            }

            names.Add(Path.GetFileNameWithoutExtension(plugin.Commands.Launch.Executable));
        }

        return names;
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
