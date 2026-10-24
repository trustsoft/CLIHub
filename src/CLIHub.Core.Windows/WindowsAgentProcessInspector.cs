namespace CLIHub.Core.Windows;

using System.Diagnostics;

using CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

using Microsoft.Extensions.Logging;

/// <summary>
///   Windows-specific implementation using System.Diagnostics.Process API. Under
///   <see cref="AgentProcessMatchDetail.CommandLine" /> detail it additionally joins command
///   lines onto the candidate processes with one bulk WMI query through an
///   <see cref="IProcessCommandLineSource" />, bounded by a hard wall-clock budget; when the
///   query fails or exceeds the budget the pass degrades to executable names (command lines
///   stay null).
/// </summary>
public sealed class WindowsAgentProcessInspector : IAgentProcessInspector
{
    /// <summary>
    ///   The hard wall-clock budget for the bulk command-line query. Synchronous WMI has no
    ///   reliable cancellation, so the wait bounds the call instead; a query that exceeds it is
    ///   abandoned to complete in the background.
    /// </summary>
    private static readonly TimeSpan DefaultCommandLineTimeout = TimeSpan.FromSeconds(2);

    private readonly IProcessCommandLineSource commandLineSource;
    private readonly ILogger<WindowsAgentProcessInspector> logger;
    private readonly TimeSpan commandLineTimeout;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsAgentProcessInspector" /> class.
    /// </summary>
    /// <param name="logger"> Logger for diagnostics. </param>
    /// <param name="commandLineSource"> The bulk command-line retrieval source. </param>
    /// <param name="commandLineTimeout">
    ///   The command-line retrieval budget; defaults to 2 seconds. Tests inject a shorter budget.
    /// </param>
    internal WindowsAgentProcessInspector(
        ILogger<WindowsAgentProcessInspector> logger,
        IProcessCommandLineSource commandLineSource,
        TimeSpan? commandLineTimeout = null)
    {
        this.logger = logger;
        this.commandLineSource = commandLineSource;
        this.commandLineTimeout = commandLineTimeout ?? DefaultCommandLineTimeout;
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentProcessInstance> GetRunningProcesses(
        IReadOnlySet<string> candidateProcessNames,
        AgentProcessMatchDetail detail)
    {
        ArgumentNullException.ThrowIfNull(candidateProcessNames);

        var instances = new List<AgentProcessInstance>();
        var seenCandidateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                try
                {
                    // Skip system processes (PID < 100 typically)
                    if (process.Id < 100)
                    {
                        continue;
                    }

                    // Candidate-name pre-filter: ProcessName arrives with the snapshot as the image
                    // file name without extension, so processes that cannot match any launch
                    // executable are skipped before the expensive MainModule/StartTime reads.
                    // Assumption A1 (fix-settings-open-delay design): ProcessName equals
                    // GetFileNameWithoutExtension(MainModule.FileName) for every process whose
                    // main module is accessible.
                    if (!candidateProcessNames.Contains(process.ProcessName))
                    {
                        continue;
                    }

                    seenCandidateNames.Add(process.ProcessName);

                    // Try to get executable path - may fail for protected processes
                    string? executablePath = null;
                    try
                    {
                        executablePath = process.MainModule?.FileName;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        // Access denied to MainModule - skip this process
                        // We only track processes with accessible executable paths
                        continue;
                    }

                    if (string.IsNullOrEmpty(executablePath))
                    {
                        continue;
                    }

                    // Try to get start time - may fail for some processes
                    DateTime startTime;
                    try
                    {
                        startTime = process.StartTime;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        // Cannot access start time - use current time as fallback
                        startTime = DateTime.Now;
                    }

                    instances.Add(new AgentProcessInstance(
                        process.Id,
                        executablePath,
                        startTime));
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // Access denied or process exited - skip silently
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to enumerate processes");
        }

        // Command lines are retrieved only under CommandLine detail, in one bulk query for the
        // candidate-named processes seen; under ExecutableName detail no retrieval occurs.
        IReadOnlyDictionary<int, string>? commandLines = null;
        if (detail == AgentProcessMatchDetail.CommandLine && seenCandidateNames.Count > 0)
        {
            commandLines = this.TryGetCommandLines(seenCandidateNames);
        }

        if (commandLines is null)
        {
            return instances;
        }

        return instances
            .Select(instance => instance with
            {
                CommandLine = commandLines.TryGetValue(instance.ProcessId, out var commandLine) ? commandLine : null
            })
            .ToList();
    }

    /// <summary>
    ///   Runs the bulk command-line query inside the hard wall-clock budget. On timeout the
    ///   abandoned query is left to complete in the background with its exception
    ///   observed-and-swallowed; on any failure or timeout the pass degrades to executable names
    ///   with one warning logged.
    /// </summary>
    /// <param name="processNames"> The candidate-named processes seen in the snapshot. </param>
    /// <returns>
    ///   The process-id → command-line map, or null when retrieval failed or timed out.
    /// </returns>
    private IReadOnlyDictionary<int, string>? TryGetCommandLines(IReadOnlySet<string> processNames)
    {
        var queryTask = Task.Run(() => this.commandLineSource.GetCommandLines(processNames));

        bool completed;
        try
        {
            completed = queryTask.Wait(this.commandLineTimeout);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(
                ex,
                "Process command-line retrieval failed or timed out; matching degrades to executable names");
            return null;
        }

        if (!completed)
        {
            this.logger.LogWarning(
                "Process command-line retrieval failed or timed out; matching degrades to executable names");

            // Observe-and-swallow the abandoned query's eventual fault so it never surfaces as
            // an unobserved task exception.
            _ = queryTask.ContinueWith(
                abandoned => _ = abandoned.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return null;
        }

        return queryTask.Result;
    }
}
