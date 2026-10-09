# Specification

## Models

### AgentProcessInstance
**File:** `src/CLIHub.Core/Models/AgentProcessInstance.cs`

```csharp
namespace CLIHub.Core.Models;

/// <summary>
///   A snapshot of a running process that may belong to an agent.
/// </summary>
public sealed record AgentProcessInstance(
    int ProcessId,
    string ExecutablePath,
    string CommandLine,
    DateTime StartTime
);
```

### RunningAgent
**File:** `src/CLIHub.Core/Models/RunningAgent.cs`

```csharp
namespace CLIHub.Core.Models;

/// <summary>
///   An agent with a confirmed running process instance.
/// </summary>
public sealed record RunningAgent(
    Agent Agent,
    AgentProcessInstance ProcessInstance
);
```

## Interfaces

### IAgentProcessInspector
**File:** `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs`

```csharp
namespace CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

/// <summary>
///   Detects running processes on the system.
/// </summary>
public interface IAgentProcessInspector
{
    /// <summary>
    ///   Enumerates all running processes accessible to the current user.
    /// </summary>
    /// <returns> List of process instances. Empty if no processes found or access denied. </returns>
    IReadOnlyList<AgentProcessInstance> GetRunningProcesses();
}
```

## Implementations

### WindowsAgentProcessInspector
**File:** `src/CLIHub.Core/Infrastructure/Processes/WindowsAgentProcessInspector.cs`

```csharp
namespace CLIHub.Core.Infrastructure.Processes;

using System.Diagnostics;
using System.Management;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
///   Windows-specific implementation using System.Diagnostics.Process API.
/// </summary>
public sealed class WindowsAgentProcessInspector : IAgentProcessInspector
{
    private readonly ILogger<WindowsAgentProcessInspector> _logger;

    public WindowsAgentProcessInspector(ILogger<WindowsAgentProcessInspector> logger)
    {
        _logger = logger;
    }

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

                    var executablePath = process.MainModule?.FileName;
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        continue;
                    }

                    var commandLine = GetCommandLine(process.Id);
                    var startTime = process.StartTime;

                    instances.Add(new AgentProcessInstance(
                        process.Id,
                        executablePath,
                        commandLine ?? string.Empty,
                        startTime));
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
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
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                return obj["CommandLine"]?.ToString();
            }
        }
        catch
        {
            // WMI access may fail - return null
        }

        return null;
    }
}
```

**Dependencies:**
- Add `System.Management` NuGet package to `CLIHub.Core.csproj`

### AgentProcessMatcher
**File:** `src/CLIHub.Core/Services/AgentProcessMatcher.cs`

```csharp
namespace CLIHub.Core.Services;

using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
///   Matches running processes to registered agents.
/// </summary>
public sealed class AgentProcessMatcher
{
    private readonly ILogger<AgentProcessMatcher> _logger;

    public AgentProcessMatcher(ILogger<AgentProcessMatcher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///   Attempts to match a process instance to an agent.
    /// </summary>
    /// <param name="processInstance"> The process to match. </param>
    /// <param name="agent"> The agent to match against. </param>
    /// <returns> True if the process belongs to this agent. </returns>
    public bool IsMatch(AgentProcessInstance processInstance, Agent agent)
    {
        // Direct executable match
        if (PathEquals(processInstance.ExecutablePath, agent.ExecutablePath))
        {
            return true;
        }

        // Runtime-based agent match (Node, Python, etc.)
        if (agent.Runtime != null)
        {
            // Check if process is the runtime executable
            var runtimeMatches = PathEquals(processInstance.ExecutablePath, agent.Runtime.ExecutablePath);
            
            // Check if command line contains the agent's script/entry point
            var scriptInCommandLine = !string.IsNullOrEmpty(processInstance.CommandLine) &&
                processInstance.CommandLine.Contains(agent.ExecutablePath, StringComparison.OrdinalIgnoreCase);

            return runtimeMatches && scriptInCommandLine;
        }

        return false;
    }

    private static bool PathEquals(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
        {
            return false;
        }

        // Windows paths are case-insensitive
        return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
    }
}
```

### AgentProcessMonitor
**File:** `src/CLIHub.Core/Services/AgentProcessMonitor.cs`

```csharp
namespace CLIHub.Core.Services;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
///   Monitors running agent processes for update safety checks.
/// </summary>
public sealed class AgentProcessMonitor
{
    private readonly IAgentProcessInspector _processInspector;
    private readonly AgentProcessMatcher _processMatcher;
    private readonly IAgentDetectionService _agentDetection;
    private readonly ILogger<AgentProcessMonitor> _logger;

    public AgentProcessMonitor(
        IAgentProcessInspector processInspector,
        AgentProcessMatcher processMatcher,
        IAgentDetectionService agentDetection,
        ILogger<AgentProcessMonitor> logger)
    {
        _processInspector = processInspector;
        _processMatcher = processMatcher;
        _agentDetection = agentDetection;
        _logger = logger;
    }

    /// <summary>
    ///   Checks if any registered agents are currently running.
    /// </summary>
    /// <returns> True if at least one agent process is running. </returns>
    public bool HasRunningAgents()
    {
        return GetRunningAgents().Count > 0;
    }

    /// <summary>
    ///   Gets the list of currently running agents with their process details.
    /// </summary>
    /// <returns> List of running agents. Empty if none are running. </returns>
    public IReadOnlyList<RunningAgent> GetRunningAgents()
    {
        var runningAgents = new List<RunningAgent>();

        try
        {
            var processes = _processInspector.GetRunningProcesses();
            var agents = _agentDetection.GetAvailableAgents();

            foreach (var process in processes)
            {
                foreach (var agent in agents)
                {
                    if (_processMatcher.IsMatch(process, agent))
                    {
                        runningAgents.Add(new RunningAgent(agent, process));
                        break; // One match per process
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for running agents");
        }

        return runningAgents;
    }
}
```

## Command Changes

### UpdateCommand
**File:** `src/CLIHub/Commands/UpdateCommand.cs`

**Changes:**
1. Add `AgentProcessMonitor` dependency to constructor
2. Update `CanExecute()` to check for running agents
3. Add double-check before starting update

```csharp
public sealed class UpdateCommand : ICommand
{
    private readonly IUpdateService _updateService;
    private readonly AgentProcessMonitor _processMonitor; // NEW

    public UpdateCommand(
        IUpdateService updateService,
        AgentProcessMonitor processMonitor) // NEW
    {
        _updateService = updateService;
        _processMonitor = processMonitor;
    }

    public bool CanExecute(object? parameter)
    {
        // Block updates when agents are running
        return !_processMonitor.HasRunningAgents();
    }

    public async Task ExecuteAsync(object? parameter)
    {
        // Double-check before starting (processes might start between check and execution)
        if (_processMonitor.HasRunningAgents())
        {
            var runningAgents = _processMonitor.GetRunningAgents();
            var agentNames = string.Join(", ", runningAgents.Select(a => a.Agent.Name));
            
            MessageBox.Show(
                $"Cannot update while agents are running.\n\nRunning agents:\n{agentNames}",
                "Update Blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            
            return;
        }

        // Proceed with update...
    }
}
```

## DI Registration

**File:** `src/CLIHub/App.xaml.cs`

Add to `ConfigureServices`:
```csharp
// Process monitoring (after IAgentDetectionService registration)
services.AddSingleton<IAgentProcessInspector, WindowsAgentProcessInspector>();
services.AddSingleton<AgentProcessMatcher>();
services.AddSingleton<AgentProcessMonitor>();
```

## NuGet Package

**File:** `src/CLIHub.Core/CLIHub.Core.csproj`

Add package reference:
```xml
<PackageReference Include="System.Management" Version="8.0.0" />
```

## Testing Strategy

### Unit Tests

**WindowsAgentProcessInspectorTests** (~5 tests):
- Returns empty list when no processes accessible
- Filters out system processes (PID < 100)
- Handles access denied gracefully
- Extracts executable path correctly
- Handles process exit during enumeration

**AgentProcessMatcherTests** (~8 tests):
- Matches direct executable agents
- Matches runtime-based agents (Node)
- Matches runtime-based agents (Python)
- Case-insensitive path comparison
- No match when paths differ
- No match when runtime differs
- No match when script not in command line
- Handles null/empty command lines

**AgentProcessMonitorTests** (~5 tests):
- HasRunningAgents returns false when no matches
- HasRunningAgents returns true when agent running
- GetRunningAgents returns empty when no matches
- GetRunningAgents returns matched agents
- Handles exceptions gracefully

### Integration Tests

**UpdateCommandTests** (~3 tests):
- CanExecute returns false when agents running
- CanExecute returns true when no agents running
- ExecuteAsync shows blocking message when agents running

## UI Changes (Future Enhancement)

UpdateControl can optionally show running agents list:
- Tooltip with agent names
- Expandable detail section
- Real-time refresh (if needed)

**Not in scope for this phase** - focus on blocking functionality first.
