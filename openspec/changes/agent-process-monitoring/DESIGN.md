# Design

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│ UpdateCommand                                       │
│ - CanExecute checks AgentProcessMonitor             │
│ - Shows blocking message if agents running          │
└─────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────┐
│ AgentProcessMonitor (Orchestrator)                  │
│ - HasRunningAgents() → bool                         │
│ - GetRunningAgents() → IReadOnlyList<RunningAgent>  │
└─────────────────────────────────────────────────────┘
                           │
                ┌──────────┴──────────┐
                ▼                     ▼
┌──────────────────────────┐ ┌──────────────────────────┐
│ IAgentProcessInspector   │ │ AgentProcessMatcher      │
│ - GetRunningProcesses()  │ │ - MatchProcess(process,  │
│                          │ │     agents) → Agent?     │
└──────────────────────────┘ └──────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────────┐
│ WindowsAgentProcessInspector (Implementation)        │
│ - Uses Process.GetProcesses()                        │
│ - Filters by executable paths                        │
└──────────────────────────────────────────────────────┘
```

## Component Responsibilities

### 1. AgentProcessInstance (Model)
**Location:** `src/CLIHub.Core/Models/AgentProcessInstance.cs`

Immutable record representing a running process instance:
```csharp
public sealed record AgentProcessInstance(
    int ProcessId,
    string ExecutablePath,
    string CommandLine,
    DateTime StartTime
);
```

### 2. IAgentProcessInspector (Interface)
**Location:** `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs`

Contract for detecting running processes:
```csharp
public interface IAgentProcessInspector
{
    IReadOnlyList<AgentProcessInstance> GetRunningProcesses();
}
```

### 3. WindowsAgentProcessInspector (Implementation)
**Location:** `src/CLIHub.Core/Infrastructure/Processes/WindowsAgentProcessInspector.cs`

Platform-specific implementation using `System.Diagnostics.Process`:
- Calls `Process.GetProcesses()`
- Filters out system processes
- Extracts executable path, command line, start time
- Handles access denied exceptions gracefully

### 4. RunningAgent (Model)
**Location:** `src/CLIHub.Core/Models/RunningAgent.cs`

Represents matched agent with process details:
```csharp
public sealed record RunningAgent(
    Agent Agent,
    AgentProcessInstance ProcessInstance
);
```

### 5. AgentProcessMatcher (Service)
**Location:** `src/CLIHub.Core/Services/AgentProcessMatcher.cs`

Matches processes to registered agents:
- Compares executable paths (handles both direct and runtime-based agents)
- For Node/Python agents: matches both runtime executable AND script path
- Case-insensitive path comparison on Windows
- Returns null if no match found

### 6. AgentProcessMonitor (Orchestrator)
**Location:** `src/CLIHub.Core/Services/AgentProcessMonitor.cs`

High-level API for update command:
- `HasRunningAgents()` - quick check for blocking
- `GetRunningAgents()` - detailed list for UI display
- Orchestrates inspector + matcher + agent detection service

## Integration Points

### UpdateCommand Changes
**File:** `src/CLIHub/Commands/UpdateCommand.cs`

```csharp
private readonly AgentProcessMonitor _processMonitor;

public UpdateCommand(
    IUpdateService updateService,
    AgentProcessMonitor processMonitor) // NEW
{
    _updateService = updateService;
    _processMonitor = processMonitor;
}

public bool CanExecute()
{
    return !_processMonitor.HasRunningAgents(); // NEW CHECK
}
```

### UpdateControl Changes
**File:** `src/CLIHub/Controls/UpdateControl.xaml.cs`

Add blocking message display when agents are running:
```csharp
private void RefreshCanUpdate()
{
    var runningAgents = _processMonitor.GetRunningAgents();
    if (runningAgents.Count > 0)
    {
        StatusText = $"Cannot update: {runningAgents.Count} agent(s) running";
        // Show detailed list in tooltip or expandable section
    }
}
```

## Matching Algorithm

### For Direct Executables
```
Process: C:\Users\X\AppData\Local\aider\aider.exe
Agent:   C:\Users\X\AppData\Local\aider\aider.exe
Match:   ✓ (exact path match)
```

### For Runtime-Based Agents (Node, Python, etc.)
```
Process: C:\Program Files\nodejs\node.exe "C:\npm\prefix\cline.js"
Agent:   Node runtime + C:\npm\prefix\cline.js
Match:   ✓ (runtime matches AND script path in command line)
```

### Edge Cases
- Symlinks: Resolve to canonical path before comparing
- Case sensitivity: Windows paths are case-insensitive
- Spaces in paths: Handle quoted paths in command line
- Access denied: Skip processes we can't inspect (system processes)

## DI Registration

**File:** `src/CLIHub/App.xaml.cs`

```csharp
// Process monitoring
services.AddSingleton<IAgentProcessInspector, WindowsAgentProcessInspector>();
services.AddSingleton<AgentProcessMatcher>();
services.AddSingleton<AgentProcessMonitor>();
```

## Error Handling

- **Access Denied:** Skip process, log warning, continue
- **Process Exited:** Ignore (race condition during enumeration)
- **Invalid Path:** Skip process, log debug message
- **Null/Empty CommandLine:** Use executable path only

## Performance Considerations

- Process enumeration is ~10-50ms on typical system
- Called only when user checks for updates (not continuous)
- No caching needed (process state changes rapidly)
- Lazy evaluation: only enumerate when needed
