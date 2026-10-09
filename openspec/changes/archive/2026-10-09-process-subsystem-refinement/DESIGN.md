# Process Subsystem Refinement Design

## Overview

Refactor `ProcessLauncher` by extracting three focused components:
1. **RuntimeSelector** - detects and selects appropriate runtime
2. **InteractiveProcessRunner** - handles interactive process launches
3. **OutputCaptureRunner** - handles output capture with timeout

## Current Architecture

```
ProcessLauncher (monolithic)
├── Runtime detection (Windows Terminal/CMD/PowerShell)
├── Interactive process launching
├── Output capture with timeout
└── Command line building coordination
```

## Target Architecture

```
ProcessLauncher (coordinator)
├── RuntimeSelector (detection & selection)
├── IInteractiveProcessRunner (interactive launches)
│   └── WindowsInteractiveProcessRunner
├── IProcessOutputRunner (output capture)
│   └── WindowsProcessOutputRunner
└── WindowsCommandLineBuilder (unchanged)
```

## Component Responsibilities

### RuntimeSelector
- **Purpose:** Detect available runtimes and select the best one
- **Input:** None (reads environment)
- **Output:** `RuntimeInfo` with executable path and type
- **Logic:**
  - Check for Windows Terminal (wt.exe in PATH)
  - Fall back to CMD if not found
  - Cache detection results
- **No external dependencies**

### IInteractiveProcessRunner / WindowsInteractiveProcessRunner
- **Purpose:** Launch interactive processes that attach to user console
- **Input:** Command line string, runtime info
- **Output:** Exit code
- **Logic:**
  - Create ProcessStartInfo with UseShellExecute = false
  - Inherit stdin/stdout/stderr
  - Wait for exit
- **Dependencies:** None (uses System.Diagnostics.Process)

### IProcessOutputRunner / WindowsProcessOutputRunner
- **Purpose:** Launch processes and capture output with timeout
- **Input:** Command line string, runtime info, timeout
- **Output:** ProcessResult with output, error, exit code, timeout flag
- **Logic:**
  - Create ProcessStartInfo with output redirection
  - Start async output/error reading
  - Wait with timeout
  - Kill process if timeout exceeded
- **Dependencies:** None (uses System.Diagnostics.Process)

### ProcessLauncher (refactored)
- **Purpose:** Coordinate runtime selection and process launching
- **Dependencies:**
  - RuntimeSelector
  - IInteractiveProcessRunner
  - IProcessOutputRunner
  - WindowsCommandLineBuilder
- **Public API:** Unchanged
  - `LaunchInteractiveAsync(command, args, workingDir)`
  - `RunWithOutputCaptureAsync(command, args, workingDir, timeout)`

## Interface Definitions

```csharp
public interface IInteractiveProcessRunner
{
    Task<int> LaunchAsync(string commandLine, RuntimeInfo runtime, CancellationToken cancellationToken = default);
}

public interface IProcessOutputRunner
{
    Task<ProcessResult> RunAsync(string commandLine, RuntimeInfo runtime, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public sealed class RuntimeInfo
{
    public string ExecutablePath { get; init; }
    public RuntimeType Type { get; init; }
}

public enum RuntimeType
{
    WindowsTerminal,
    Cmd,
    PowerShell
}
```

## Migration Strategy

1. **Phase 1:** Create new components with tests
   - RuntimeSelector
   - WindowsInteractiveProcessRunner
   - WindowsProcessOutputRunner

2. **Phase 2:** Update ProcessLauncher to use new components
   - Keep public API unchanged
   - Replace inline logic with component calls
   - Verify all existing tests pass

3. **Phase 3:** Add integration tests for the composition

## Testing Strategy

### Unit Tests
- **RuntimeSelector:**
  - Detects Windows Terminal when available
  - Falls back to CMD when WT not found
  - Caches detection results

- **WindowsInteractiveProcessRunner:**
  - Launches process with correct command line
  - Returns exit code
  - Handles cancellation

- **WindowsProcessOutputRunner:**
  - Captures stdout and stderr
  - Respects timeout
  - Kills process on timeout
  - Returns correct timeout flag

### Integration Tests
- **ProcessLauncher:**
  - All existing tests continue to pass
  - New tests for error handling paths

## Backward Compatibility

- All public APIs remain unchanged
- Existing tests continue to pass without modification
- Internal implementation changes only

## Non-Goals

- Adding support for new runtime types (can be added later)
- Changing timeout defaults
- Modifying command line building logic
- Cross-platform support (Linux/Mac)
