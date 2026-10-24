namespace CLIHub.Core.Windows;

using System.Management;

using Microsoft.Extensions.Logging;

/// <summary>
///   Supplies command lines for running processes by process id. Implementations perform one
///   bulk query for the given process names.
/// </summary>
internal interface IProcessCommandLineSource
{
    /// <summary>
    ///   Retrieves a process-id → command-line map for the given process names in one bulk query.
    /// </summary>
    /// <param name="processNames">
    ///   The process names (image file names without extension, compared ignoring case) whose
    ///   command lines are retrieved.
    /// </param>
    /// <returns>
    ///   A map of process id to command line; entries may be missing when a command line is
    ///   unreadable or the process has exited between enumeration and retrieval.
    /// </returns>
    IReadOnlyDictionary<int, string> GetCommandLines(IReadOnlySet<string> processNames);
}

/// <summary>
///   Retrieves process command lines with a single WMI <c>Win32_Process</c> query over the
///   supplied process names. Best effort: command lines that WMI cannot read (elevated or
///   other-session processes) are simply missing from the returned map; query failures propagate
///   to the caller's degradation path.
/// </summary>
internal sealed class WmiProcessCommandLineSource : IProcessCommandLineSource
{
    // Best-effort hint only: synchronous WMI has no reliable cancellation, so the authoritative
    // budget is the inspector's hard wall-clock wait (design D6).
    private static readonly TimeSpan QueryTimeoutHint = TimeSpan.FromSeconds(2);

    private readonly ILogger<WmiProcessCommandLineSource> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WmiProcessCommandLineSource" /> class.
    /// </summary>
    /// <param name="logger"> Logger for diagnostics. </param>
    public WmiProcessCommandLineSource(ILogger<WmiProcessCommandLineSource> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, string> GetCommandLines(IReadOnlySet<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);

        var commandLines = new Dictionary<int, string>();

        if (processNames.Count == 0)
        {
            return commandLines;
        }

        // Win32_Process.Name is the image file name including the .exe extension; the candidate
        // names arrive without one. WQL string comparisons are case-insensitive.
        var condition = string.Join(
            " OR ",
            processNames.Select(name => $"Name = '{name.Replace("'", "''", StringComparison.Ordinal)}.exe'"));
        var query = new SelectQuery("Win32_Process", condition, ["ProcessId", "CommandLine"]);

        using var searcher = new ManagementObjectSearcher(query)
        {
            Options = new EnumerationOptions { Timeout = QueryTimeoutHint }
        };

        using var results = searcher.Get();

        foreach (var result in results)
        {
            using var process = (ManagementBaseObject)result;
            if (process["ProcessId"] is not uint processId
                || process["CommandLine"] is not string commandLine
                || commandLine.Length == 0)
            {
                continue;
            }

            commandLines[(int)processId] = commandLine;
        }

        this.logger.LogDebug("WMI command-line query returned {Count} command lines", commandLines.Count);
        return commandLines;
    }
}
