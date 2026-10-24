namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Matches running processes to registered agent plugins by the launch executable's file name
///   and, for agents distributed as npm packages, by a runtime-host process's command line.
/// </summary>
public sealed class AgentProcessMatcher
{
    /// <summary>
    ///   Process names of the known shared runtime hosts (Node.js and Python) that npm shims and
    ///   python entry points spawn agents inside (design decision: npm shims and python entry
    ///   points; shells such as <c>pwsh</c> are deliberately excluded because CLIHub itself spawns
    ///   them constantly and their command lines carry no package marker). Compared ignoring case.
    /// </summary>
    public static IReadOnlySet<string> RuntimeHostProcessNames { get; } =
        new HashSet<string>(["node", "python", "pythonw"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///   Derives the candidate process names for the given plugins: the launch executable's file
    ///   name without extension, which is exactly the comparison basis <see cref="IsMatch" /> uses.
    ///   Under <see cref="AgentProcessMatchDetail.CommandLine" /> detail, the shared runtime host
    ///   names (<see cref="RuntimeHostProcessNames" />) are added whenever at least one plugin
    ///   declares both a launch command and an npm <c>updateCheck</c> package, so the inspector
    ///   pre-filter cannot drop a runtime-hosted process the matcher would accept.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The process inspector port pre-filters processes by this set before reading expensive
    ///     per-process details such as the main module. A process can match a plugin only when its
    ///     executable file name equals one of these candidates or — under command-line detail —
    ///     it is hosted inside one of the runtime hosts these candidates include exactly when
    ///     command-line matching is active, so the filter cannot drop a match the matcher would
    ///     make.
    ///   </para>
    /// </remarks>
    /// <param name="plugins"> The plugins whose launch executables define the candidates. </param>
    /// <param name="detail">
    ///   The matching fidelity the enumeration will run at. Runtime host names join the candidates
    ///   only under <see cref="AgentProcessMatchDetail.CommandLine" /> and only when an npm-declared
    ///   plugin makes command-line matching possible.
    /// </param>
    /// <returns>
    ///   Candidate process names compared ordinally, ignoring case. Plugins without a launch
    ///   command contribute nothing.
    /// </returns>
    public IReadOnlySet<string> GetCandidateProcessNames(
        IEnumerable<Plugin> plugins,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var hasNpmDeclaredPlugin = false;

        foreach (var plugin in plugins)
        {
            if (plugin.Commands?.Launch is null)
            {
                continue;
            }

            names.Add(Path.GetFileNameWithoutExtension(plugin.Commands.Launch.Executable));

            if (!hasNpmDeclaredPlugin && DeclaresNpmUpdateCheck(plugin))
            {
                hasNpmDeclaredPlugin = true;
            }
        }

        if (detail == AgentProcessMatchDetail.CommandLine && hasNpmDeclaredPlugin)
        {
            names.UnionWith(RuntimeHostProcessNames);
        }

        return names;
    }

    /// <summary>
    ///   Attempts to match a process instance to a plugin.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A process matches a plugin when either rule holds. Executable-name rule: the process
    ///     executable's file name equals the plugin's declared launch executable's file name,
    ///     ignoring case. Command-line rule (design D5): the plugin has a launch command and
    ///     declares an npm <c>updateCheck</c> with a non-empty package; the process runs inside a
    ///     shared runtime host (<see cref="RuntimeHostProcessNames" />); and the process's command
    ///     line — normalized by replacing forward slashes with backslashes — contains
    ///     <c>node_modules\</c> followed by the package name (likewise normalized) followed by a
    ///     trailing separator, compared ignoring case. The trailing separator is the boundary that
    ///     keeps a package name that is a prefix of another package's name from matching.
    ///   </para>
    ///   <para>
    ///     A process hosted inside a shared runtime without a readable command line (retrieval
    ///     degraded or unavailable) never command-line-matches; the executable-name rule alone
    ///     applies, which cannot match a host-named process unless the plugin's launch executable
    ///     is the host itself.
    ///   </para>
    /// </remarks>
    /// <param name="processInstance"> The process to match. </param>
    /// <param name="plugin"> The plugin to match against. </param>
    /// <param name="detail">
    ///   The matching fidelity of the enumeration. Under
    ///   <see cref="AgentProcessMatchDetail.ExecutableName" /> only the executable-name rule
    ///   applies — the command-line rule is not evaluated even when a command line is present.
    /// </param>
    /// <returns>
    ///   True if the process matches the plugin by executable name or, under
    ///   <see cref="AgentProcessMatchDetail.CommandLine" />, by runtime-host command line.
    /// </returns>
    public bool IsMatch(
        AgentProcessInstance processInstance,
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    {
        if (plugin.Commands?.Launch == null)
        {
            return false;
        }

        var executableName = Path.GetFileNameWithoutExtension(plugin.Commands.Launch.Executable);
        var processName = Path.GetFileNameWithoutExtension(processInstance.ExecutablePath);

        if (string.Equals(processName, executableName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return detail == AgentProcessMatchDetail.CommandLine
            && MatchesRuntimeHostCommandLine(processInstance, plugin);
    }

    /// <summary>
    ///   Determines whether the plugin declares an npm <c>updateCheck</c> with a non-empty package
    ///   — the command-line matching marker (source kind compared ignoring case, matching
    ///   <see cref="Agents.AgentReleaseClient" /> precedent).
    /// </summary>
    /// <param name="plugin"> The plugin to inspect. </param>
    /// <returns> True when the plugin declares an npm update check with a package name. </returns>
    private static bool DeclaresNpmUpdateCheck(Plugin plugin)
    {
        var updateCheck = plugin.UpdateCheck;

        return updateCheck is not null
            && string.Equals(updateCheck.Source, "npm", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(updateCheck.Package);
    }

    /// <summary>
    ///   Applies the runtime-host command-line rule of design D5 against the process command line.
    /// </summary>
    /// <param name="processInstance"> The process to match. </param>
    /// <param name="plugin"> The plugin to match against. </param>
    /// <returns> True when the command line names the plugin's declared npm package. </returns>
    private static bool MatchesRuntimeHostCommandLine(AgentProcessInstance processInstance, Plugin plugin)
    {
        if (!DeclaresNpmUpdateCheck(plugin))
        {
            return false;
        }

        var processName = Path.GetFileNameWithoutExtension(processInstance.ExecutablePath);
        if (!RuntimeHostProcessNames.Contains(processName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(processInstance.CommandLine))
        {
            return false;
        }

        var package = plugin.UpdateCheck!.Package!.Replace('/', '\\');
        var marker = $"node_modules\\{package}\\";
        var normalizedCommandLine = processInstance.CommandLine.Replace('/', '\\');

        return normalizedCommandLine.Contains(marker, StringComparison.OrdinalIgnoreCase);
    }
}
