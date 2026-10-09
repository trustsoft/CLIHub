# Process Subsystem Refinement Specification

## Components

### 1. RuntimeSelector

**Location:** `src/CLIHub.Core/Infrastructure/Processes/RuntimeSelector.cs`

**Purpose:** Detect and select the appropriate command-line runtime on Windows.

**Public API:**
```csharp
public sealed class RuntimeSelector
{
    public RuntimeInfo SelectRuntime();
}

public sealed class RuntimeInfo
{
    public required string ExecutablePath { get; init; }
    public required RuntimeType Type { get; init; }
}

public enum RuntimeType
{
    WindowsTerminal,
    Cmd,
    PowerShell
}
```

**Behavior:**
- First call checks if `wt.exe` (Windows Terminal) is in PATH
- If found, returns RuntimeInfo with Type=WindowsTerminal, ExecutablePath="wt.exe"
- If not found, falls back to CMD with ExecutablePath="cmd.exe", Type=Cmd
- Result is cached after first call
- Thread-safe

**Error Handling:**
- Never throws
- Always returns a valid runtime (CMD as fallback)

---

### 2. IInteractiveProcessRunner

**Location:** `src/CLIHub.Core/Infrastructure/Processes/IInteractiveProcessRunner.cs`

**Purpose:** Contract for launching interactive processes that attach to user console.

**API:**
```csharp
public interface IInteractiveProcessRunner
{
    Task<int> LaunchAsync(
        string commandLine, 
        RuntimeInfo runtime, 
        CancellationToken cancellationToken = default);
}
```

**Parameters:**
- `commandLine`: Full command line to execute (already quoted/escaped)
- `runtime`: Runtime information from RuntimeSelector
- `cancellationToken`: Optional cancellation token

**Returns:**
- Exit code of the launched process

**Behavior Contract:**
- Process must inherit stdin/stdout/stderr from current process
- Process must run to completion
- Must return actual exit code
- Must respect cancellation token

---

### 3. WindowsInteractiveProcessRunner

**Location:** `src/CLIHub.Core/Infrastructure/Processes/WindowsInteractiveProcessRunner.cs`

**Purpose:** Windows implementation of IInteractiveProcessRunner.

**API:**
```csharp
public sealed class WindowsInteractiveProcessRunner : IInteractiveProcessRunner
{
    public Task<int> LaunchAsync(
        string commandLine, 
        RuntimeInfo runtime, 
        CancellationToken cancellationToken = default);
}
```

**Implementation Details:**
- Creates ProcessStartInfo with:
  - FileName = runtime.ExecutablePath
  - Arguments = commandLine
  - UseShellExecute = false
  - RedirectStandardInput/Output/Error = false (inherit from parent)
  - CreateNoWindow = false
- Starts process
- Waits for exit (respecting cancellation)
- Returns exit code

**Error Handling:**
- Throws `InvalidOperationException` if process fails to start
- Throws `OperationCanceledException` if cancelled
- Logs failures via ILogger

**Dependencies:**
- ILogger<WindowsInteractiveProcessRunner>

---

### 4. IProcessOutputRunner

**Location:** `src/CLIHub.Core/Infrastructure/Processes/IProcessOutputRunner.cs`

**Purpose:** Contract for running processes with output capture and timeout.

**API:**
```csharp
public interface IProcessOutputRunner
{
    Task<ProcessResult> RunAsync(
        string commandLine,
        RuntimeInfo runtime,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessResult
{
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
    public required int ExitCode { get; init; }
    public required bool TimedOut { get; init; }
}
```

**Parameters:**
- `commandLine`: Full command line to execute
- `runtime`: Runtime information
- `timeout`: Maximum execution time
- `cancellationToken`: Optional cancellation

**Returns:**
- ProcessResult with captured output, error, exit code, and timeout flag

**Behavior Contract:**
- Must capture all stdout and stderr
- Must enforce timeout
- If timeout exceeded: kill process, set TimedOut=true, ExitCode=-1
- If completed normally: set TimedOut=false, actual ExitCode
- Must respect cancellation token

---

### 5. WindowsProcessOutputRunner

**Location:** `src/CLIHub.Core/Infrastructure/Processes/WindowsProcessOutputRunner.cs`

**Purpose:** Windows implementation of IProcessOutputRunner.

**API:**
```csharp
public sealed class WindowsProcessOutputRunner : IProcessOutputRunner
{
    public Task<ProcessResult> RunAsync(
        string commandLine,
        RuntimeInfo runtime,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
```

**Implementation Details:**
- Creates ProcessStartInfo with:
  - FileName = runtime.ExecutablePath
  - Arguments = commandLine
  - UseShellExecute = false
  - RedirectStandardOutput/Error = true
  - CreateNoWindow = true
- Starts async output/error reading (BeginOutputReadLine/BeginErrorReadLine)
- Waits for exit with timeout
- If timeout: kills process, returns TimedOut=true, ExitCode=-1
- If normal exit: returns captured output, TimedOut=false, actual exit code

**Error Handling:**
- Throws `InvalidOperationException` if process fails to start
- Throws `OperationCanceledException` if cancelled
- Logs all operations via ILogger

**Dependencies:**
- ILogger<WindowsProcessOutputRunner>

---

### 6. ProcessLauncher (Refactored)

**Location:** `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`

**Purpose:** Coordinate runtime selection and process launching (unchanged public API).

**Public API (unchanged):**
```csharp
public sealed class ProcessLauncher : IProcessLauncher
{
    public Task<int> LaunchInteractiveAsync(
        string command,
        string? arguments = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    public Task<ProcessResult> RunWithOutputCaptureAsync(
        string command,
        string? arguments = null,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
```

**New Internal Structure:**
```csharp
private readonly RuntimeSelector _runtimeSelector;
private readonly IInteractiveProcessRunner _interactiveRunner;
private readonly IProcessOutputRunner _outputRunner;
private readonly WindowsCommandLineBuilder _commandLineBuilder;
```

**Implementation Changes:**
- `LaunchInteractiveAsync`:
  1. Get runtime from _runtimeSelector.SelectRuntime()
  2. Build command line via _commandLineBuilder
  3. Call _interactiveRunner.LaunchAsync(commandLine, runtime, cancellationToken)
  4. Return exit code

- `RunWithOutputCaptureAsync`:
  1. Get runtime from _runtimeSelector.SelectRuntime()
  2. Build command line via _commandLineBuilder
  3. Call _outputRunner.RunAsync(commandLine, runtime, timeout ?? default, cancellationToken)
  4. Return ProcessResult

**Dependencies:**
- RuntimeSelector (new)
- IInteractiveProcessRunner (new)
- IProcessOutputRunner (new)
- WindowsCommandLineBuilder (existing, unchanged)
- ILogger<ProcessLauncher>

---

## DI Registration

**Location:** `src/CLIHub/ServiceConfiguration.cs` (or equivalent)

```csharp
services.AddSingleton<RuntimeSelector>();
services.AddSingleton<IInteractiveProcessRunner, WindowsInteractiveProcessRunner>();
services.AddSingleton<IProcessOutputRunner, WindowsProcessOutputRunner>();
services.AddSingleton<IProcessLauncher, ProcessLauncher>();
```

---

## Testing Requirements

### RuntimeSelector Tests
**Location:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/RuntimeSelectorTests.cs`

- `SelectRuntime_WhenWindowsTerminalAvailable_ReturnsWindowsTerminal`
- `SelectRuntime_WhenWindowsTerminalNotAvailable_ReturnsCmdFallback`
- `SelectRuntime_IsCached_ReturnsSameInstance`

### WindowsInteractiveProcessRunner Tests
**Location:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/WindowsInteractiveProcessRunnerTests.cs`

- `LaunchAsync_WithValidCommand_ReturnsExitCode`
- `LaunchAsync_WithInvalidCommand_ThrowsInvalidOperationException`
- `LaunchAsync_WhenCancelled_ThrowsOperationCanceledException`

### WindowsProcessOutputRunner Tests
**Location:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/WindowsProcessOutputRunnerTests.cs`

- `RunAsync_CapturesStandardOutput`
- `RunAsync_CapturesStandardError`
- `RunAsync_ReturnsCorrectExitCode`
- `RunAsync_WhenTimeout_KillsProcessAndSetsTimedOutFlag`
- `RunAsync_WhenCancelled_ThrowsOperationCanceledException`

### ProcessLauncher Integration Tests
**Location:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs`

- All existing tests must continue to pass (unchanged)
- Add: `LaunchInteractiveAsync_UsesCorrectRuntime`
- Add: `RunWithOutputCaptureAsync_UsesCorrectRuntime`

---

## Acceptance Criteria

1. ✅ All new components created with correct interfaces
2. ✅ ProcessLauncher refactored to use new components
3. ✅ All public APIs unchanged
4. ✅ All existing tests pass without modification
5. ✅ New tests added for each component (minimum 10 new tests total)
6. ✅ Code compiles without warnings
7. ✅ No breaking changes

---

## Non-Functional Requirements

- **Performance:** No measurable performance degradation
- **Logging:** All component operations logged at appropriate levels
- **Thread Safety:** RuntimeSelector must be thread-safe (cached result)
- **Error Messages:** Clear, actionable error messages for all failure cases
