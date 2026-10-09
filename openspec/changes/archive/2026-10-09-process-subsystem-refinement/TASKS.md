# Process Subsystem Refinement - Implementation Tasks

## Phase 1: Create Core Components

### Task 1.1: Create RuntimeInfo and RuntimeType
**File:** `src/CLIHub.Core/Infrastructure/Processes/RuntimeInfo.cs`
**Description:** Create the runtime information model
**Acceptance:**
- RuntimeInfo record with ExecutablePath and Type properties
- RuntimeType enum with WindowsTerminal, Cmd, PowerShell
- File-scoped namespace, nullable reference types enabled
- XML documentation on public members

---

### Task 1.2: Create RuntimeSelector
**File:** `src/CLIHub.Core/Infrastructure/Processes/RuntimeSelector.cs`
**Description:** Implement runtime detection and selection logic
**Acceptance:**
- SelectRuntime() method returns RuntimeInfo
- Checks for wt.exe in PATH
- Falls back to cmd.exe if WT not found
- Caches result after first call (thread-safe)
- Unit tests in RuntimeSelectorTests.cs (3 tests minimum)

---

### Task 1.3: Create IInteractiveProcessRunner interface
**File:** `src/CLIHub.Core/Infrastructure/Processes/IInteractiveProcessRunner.cs`
**Description:** Define contract for interactive process launching
**Acceptance:**
- LaunchAsync method with commandLine, runtime, cancellationToken parameters
- Returns Task<int> (exit code)
- XML documentation on interface and method

---

### Task 1.4: Create WindowsInteractiveProcessRunner
**File:** `src/CLIHub.Core/Infrastructure/Processes/WindowsInteractiveProcessRunner.cs`
**Description:** Implement interactive process runner for Windows
**Acceptance:**
- Implements IInteractiveProcessRunner
- Creates Process with inherited stdin/stdout/stderr
- Respects cancellation token
- Logs operations via ILogger
- Unit tests in WindowsInteractiveProcessRunnerTests.cs (3 tests minimum)

---

### Task 1.5: Create IProcessOutputRunner interface
**File:** `src/CLIHub.Core/Infrastructure/Processes/IProcessOutputRunner.cs`
**Description:** Define contract for output capture with timeout
**Acceptance:**
- RunAsync method with commandLine, runtime, timeout, cancellationToken
- Returns Task<ProcessResult>
- ProcessResult record with StandardOutput, StandardError, ExitCode, TimedOut
- XML documentation

---

### Task 1.6: Create WindowsProcessOutputRunner
**File:** `src/CLIHub.Core/Infrastructure/Processes/WindowsProcessOutputRunner.cs`
**Description:** Implement output capture runner for Windows
**Acceptance:**
- Implements IProcessOutputRunner
- Redirects stdout/stderr and captures output
- Enforces timeout (kills process if exceeded)
- Sets TimedOut flag correctly
- Respects cancellation token
- Logs operations via ILogger
- Unit tests in WindowsProcessOutputRunnerTests.cs (5 tests minimum)

---

## Phase 2: Refactor ProcessLauncher

### Task 2.1: Update ProcessLauncher constructor
**File:** `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
**Description:** Inject new dependencies
**Acceptance:**
- Add RuntimeSelector parameter
- Add IInteractiveProcessRunner parameter
- Add IProcessOutputRunner parameter
- Keep WindowsCommandLineBuilder parameter
- Update private fields

---

### Task 2.2: Refactor LaunchInteractiveAsync
**File:** `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
**Description:** Use new components for interactive launching
**Acceptance:**
- Get runtime from RuntimeSelector
- Build command line via WindowsCommandLineBuilder
- Call IInteractiveProcessRunner.LaunchAsync
- Public API unchanged
- Remove old inline process creation logic

---

### Task 2.3: Refactor RunWithOutputCaptureAsync
**File:** `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
**Description:** Use new components for output capture
**Acceptance:**
- Get runtime from RuntimeSelector
- Build command line via WindowsCommandLineBuilder
- Call IProcessOutputRunner.RunAsync
- Public API unchanged
- Remove old inline process creation logic

---

### Task 2.4: Update DI registration
**File:** `src/CLIHub/ServiceConfiguration.cs`
**Description:** Register new components in DI container
**Acceptance:**
- Register RuntimeSelector as singleton
- Register IInteractiveProcessRunner -> WindowsInteractiveProcessRunner as singleton
- Register IProcessOutputRunner -> WindowsProcessOutputRunner as singleton
- ProcessLauncher registration updated (if needed)

---

## Phase 3: Verification and Testing

### Task 3.1: Run all existing tests
**Description:** Verify backward compatibility
**Acceptance:**
- All existing ProcessLauncherTests pass without modification
- All other existing tests pass
- No breaking changes detected

---

### Task 3.2: Add integration tests
**File:** `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs`
**Description:** Add tests for the refactored composition
**Acceptance:**
- Add test: LaunchInteractiveAsync_UsesCorrectRuntime
- Add test: RunWithOutputCaptureAsync_UsesCorrectRuntime
- Both tests pass

---

### Task 3.3: Code review and cleanup
**Description:** Final review and polish
**Acceptance:**
- No compiler warnings
- All XML documentation complete
- Consistent code style (matches .editorconfig)
- Logging statements at appropriate levels
- Error messages clear and actionable

---

## Task Checklist

- [x] Task 1.1: Create RuntimeInfo and RuntimeType
- [x] Task 1.2: Create RuntimeSelector
- [x] Task 1.3: Create IInteractiveProcessRunner interface
- [x] Task 1.4: Create WindowsInteractiveProcessRunner
- [x] Task 1.5: Create IProcessOutputRunner interface
- [x] Task 1.6: Create WindowsProcessOutputRunner
- [x] Task 2.1: Update ProcessLauncher constructor
- [x] Task 2.2: Refactor LaunchInteractiveAsync
- [x] Task 2.3: Refactor RunWithOutputCaptureAsync
- [x] Task 2.4: Update DI registration
- [x] Task 3.1: Run all existing tests
- [x] Task 3.2: Add integration tests
- [x] Task 3.3: Code review and cleanup

---

## Dependencies

- Task 1.4 depends on 1.1, 1.2, 1.3
- Task 1.6 depends on 1.1, 1.2, 1.5
- Task 2.1 depends on 1.2, 1.3, 1.5
- Task 2.2 depends on 2.1
- Task 2.3 depends on 2.1
- Task 2.4 depends on 2.1, 2.2, 2.3
- Task 3.1 depends on Phase 2 complete
- Task 3.2 depends on 3.1
- Task 3.3 depends on 3.2

---

## Estimated Effort

- Phase 1: 4-6 hours (new components + tests)
- Phase 2: 2-3 hours (refactoring)
- Phase 3: 1-2 hours (verification)
- **Total: 7-11 hours**
