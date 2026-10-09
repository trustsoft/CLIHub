namespace CLIHub.Core.Infrastructure.Processes;

using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;

using CLIHub.Core.Models;

using Microsoft.Extensions.Logging;

/// <summary>
///   Windows-specific implementation using System.Diagnostics.Process API.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAgentProcessInspector : IAgentProcessInspector
{
    private readonly ILogger<WindowsAgentProcessInspector> _logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsAgentProcessInspector"/> class.
    /// </summary>
    /// <param name="logger"> Logger for diagnostics. </param>
    public WindowsAgentProcessInspector(ILogger<WindowsAgentProcessInspector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentProcessInstance> GetRunningProcesses()
    {
        var instances = new List<AgentProcessInstance>();

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

                    // Skip WMI command line retrieval for now - it can hang or be slow
                    var commandLine = string.Empty;

                    instances.Add(new AgentProcessInstance(
                        process.Id,
                        executablePath,
                        commandLine,
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
            _logger.LogWarning(ex, "Failed to enumerate processes");
        }

        return instances;
    }

    private static string? GetCommandLine(int processId)
    {
        // WMI queries can be slow or hang - disabled for now
        // TODO: Implement async WMI query with timeout or use alternative method
        return null;
    }
}
