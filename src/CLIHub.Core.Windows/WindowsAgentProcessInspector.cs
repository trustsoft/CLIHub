namespace CLIHub.Core.Windows;

using System.Diagnostics;

using CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

using Microsoft.Extensions.Logging;

/// <summary>
///   Windows-specific implementation using System.Diagnostics.Process API.
/// </summary>
public sealed class WindowsAgentProcessInspector : IAgentProcessInspector
{
    private readonly ILogger<WindowsAgentProcessInspector> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsAgentProcessInspector" /> class.
    /// </summary>
    /// <param name="logger"> Logger for diagnostics. </param>
    public WindowsAgentProcessInspector(ILogger<WindowsAgentProcessInspector> logger)
    {
        this.logger = logger;
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

                    // Matching is executable-name based; command lines are not retrieved.
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

        return instances;
    }
}
