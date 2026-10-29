## Context

See proposal.md — Why. `ProcessLauncher` was marked obsolete in commit 37e3c46 (Phase 3) after `ProcessOrchestrator` replaced it in production. Current state at commit 575e208:

**Verified current state (file:line):**

`ProcessLauncher` exists at:
- `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:1-261` — obsolete class with `[Obsolete("ProcessLauncher is obsolete. Use IProcessOrchestrator for new code.", DiagnosticId = "CLIHUB001")]` at line 14

DI registrations:
- `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:57` — `services.AddSingleton<ProcessLauncher>();`
- `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs:27-31` — test-only workaround: instantiates `ProcessLauncher` with fakes so Core-only container can resolve agent chain

Test file references with CLIHUB001 warnings (33 total, counted from build output):
- `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs:10-380` — 26 CLIHUB001 warnings, 13 tests
  - Lines 19, 46, 62, 85, 99, 117, 136, 145, 158 — `ProcessLauncher.BuildStartInfo` calls
  - Lines 193-194, 226-227, 253-254, 286-287, 313-314, 345-346, 379-380 — `new ProcessLauncher(...)` constructions
- `tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs:9-89` — 4 CLIHUB001 warnings, 5 tests
  - Lines 81-84 — `CreateRealProcessLauncher()` helper returning `new ProcessLauncher(...)`
  - Real-process integration: captures stdout, timeout, cancellation, PATHEXT resolution
- `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs:137,177` — 2 CLIHUB001 warnings
  - Line 137: `var (_, arguments) = ProcessLauncher.BuildStartInfo(RuntimeKind.PowerShell, command, shimDir, null, null);`
  - Line 177: `var (_, arguments) = ProcessLauncher.BuildStartInfo(runtimeKind, command, workingDirectory, null, powerShell7Executable);`
  - Both construct the runner directly after building arguments; can call `WindowsCommandLineBuilder.BuildInteractive` instead
- `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs:22` — 1 CLIHUB001 warning
  - `Assert.NotNull(provider.GetRequiredService<ProcessLauncher>());` — validates registration; remove the assertion

Orchestrator production API (verified at `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:10-208`):
- Constructor takes `(IRuntimeResolver, ICommandLineBuilder, IInteractiveProcessRunner, IProcessOutputRunner, IAgentProcessFinder, ILogger<ProcessOrchestrator>)`
- `Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest)` — line 45
- `Task<ProcessResult> CaptureOutputAsync(LaunchRequest)` — line 109
- `Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria)` — line 179
- `Task<IReadOnlyList<PluginAgentProcess>> FindRunningAgentsAsync(IReadOnlyList<Plugin>, AgentProcessMatchDetail)` — line 190
- `Task<RuntimeExecutable?> ResolveRuntimeAsync(RuntimeKind, CancellationToken)` — line 202

Supporting types (verified):
- `src/CLIHub.Core/Processes/RuntimeResolver.cs:9-43` — adapts `IRuntimeExecutableLocator`; returns defaults for PowerShell/CommandPrompt, null for GitBash/PowerShell7 when not located
- `src/CLIHub.Core/Processes/CommandLineBuilderAdapter.cs:9-36` — adapts `WindowsCommandLineBuilder`; extracts runtime executables and passes them through
- `src/CLIHub.Core/Infrastructure/Processes/IProcessOutputRunner.cs:10-26` — `Task<ProcessResult> RunAsync(string commandLine, RuntimeInfo, TimeSpan timeout, CancellationToken)`
- `src/CLIHub.Core/Infrastructure/Processes/ProcessResult.cs:6-27` — record with `StandardOutput`, `StandardError`, `ExitCode`, `TimedOut`
- Existing test helper: `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:30-37` — `CreateOrchestrator(IAgentProcessInspector)` internal static factory; new tests can follow the same wiring pattern

## Goals / Non-Goals

**Goals:**
- Zero CLIHUB001 warnings after removal
- Replace `ProcessLauncherTests` behavioral coverage with direct orchestrator tests
- Preserve real-process integration coverage from `ProcessLauncherCaptureOutputTests`
- All existing tests remain green (except deleted/rewritten)
- Test count increases: ~1240 after (from ~1223 before)

**Non-Goals:**
- Changing orchestrator behavior or public API
- Modifying production code beyond type/registration removal
- Adding new runtime kinds or command patterns
- Integration tests beyond porting existing real-process coverage

## Decisions

### Decision 1: Delete `ProcessLauncherTests.cs` and replace with orchestrator tests

**Chosen:** Delete `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs` entirely. Create `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs` covering the orchestrator's mapping logic with mocked dependencies (following the pattern in `AgentProcessMonitorTests.CreateOrchestrator`).

Coverage to replace:
- `BuildStartInfo` runtime mapping → covered by `CommandLineBuilderAdapterTests` and integration with the builder
- Launch success/failure → `LaunchInteractiveAsync` tests
- Capture timeout/cancellation/exit codes → `CaptureOutputAsync` tests
- Runtime validation (GitBash/PowerShell7 not located) → `RuntimeResolverTests` and orchestrator's unavailable-runtime path

**Rationale:** `ProcessLauncherTests` tests the obsolete type's static methods and instance behavior. The orchestrator now owns that contract; test it directly. The real wiring (resolver + builder + runners) is already tested by integration tests and the monitor helper.

**Alternatives considered:**
- Keep `ProcessLauncherTests` and suppress CLIHUB001 with `#pragma` → rejected: leaves obsolete code and tests in the codebase
- Port every `ProcessLauncherTests` test 1:1 to orchestrator → rejected: some tests (like `BuildStartInfo`) are now split across resolver/builder/adapter; others duplicate integration coverage

### Decision 2: Port `ProcessLauncherCaptureOutputTests` to drive `IProcessOutputRunner` directly

**Chosen:** Rewrite `tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs` to instantiate `WindowsProcessOutputRunner` directly with `cmd.exe` runtime and test the same scenarios:
- Capture stdout (`echo hello-capture`)
- Missing executable reports non-zero exit
- Cancellation terminates the process
- Timeout terminates the process
- PATHEXT resolution (`.cmd`, `.bat` shims)

Keep the file name; update the class name to `WindowsProcessOutputRunnerIntegrationTests`.

**Rationale:** The value is real-process behavior under timeout/cancellation. The orchestrator's `CaptureOutputAsync` is thin mapping; the runner integration is the irreplaceable coverage. Driving the runner directly is simpler and tests the actual Windows-specific boundary.

**Alternatives considered:**
- Delete the file → rejected: loses the only real-process integration tests for output capture
- Drive the orchestrator → rejected: adds orchestrator setup overhead for tests that validate the Windows runner, not the orchestrator

### Decision 3: Rewrite two `WindowsInteractiveProcessRunnerTests` lines to use `WindowsCommandLineBuilder`

**Chosen:** At lines 137 and 177 in `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`, replace:
```csharp
var (_, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, dir, gitBash, ps7);
```
with:
```csharp
var (_, arguments) = WindowsCommandLineBuilder.BuildInteractive(runtime, command, dir, gitBash, ps7);
```

**Rationale:** `ProcessLauncher.BuildStartInfo` is a thin wrapper over `WindowsCommandLineBuilder.BuildInteractive` (verified at `ProcessLauncher.cs:245-252`). The tests validate the runner, not the launcher; call the builder directly.

**Alternatives considered:**
- Delete the tests → rejected: they validate real PowerShell argument passing and working directory handling, irreplaceable coverage
- Keep `ProcessLauncher.BuildStartInfo` and suppress warnings → rejected: leaves obsolete code dependency

### Decision 4: Remove DI registration assertions

**Chosen:** 
- Remove line 57 from `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs`
- Remove lines 27-31 from `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs` (the test-only `ProcessLauncher` workaround; `IProcessOrchestrator` fake already registered at line 32)
- Remove line 22 from `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` (`Assert.NotNull(provider.GetRequiredService<ProcessLauncher>());`)

**Rationale:** `ProcessLauncher` is not part of the production container after removal. The Core composition test kept it only to resolve the version probe service; that chain now resolves `IProcessOrchestrator` instead (already wired at line 32). The Windows test validates the full container; removing the obsolete type assertion preserves the rest.

**Alternatives considered:** None; registration removal is mandatory when the type is deleted.

### Decision 5: Test structure for orchestrator

**Chosen:** Three test files under `tests/CLIHub.Core.Tests/Processes/`:
1. `ProcessOrchestratorTests.cs` — orchestrator's domain mapping and delegation with mocked dependencies
2. `RuntimeResolverTests.cs` — resolver's null-vs-default logic per runtime kind
3. `CommandLineBuilderAdapterTests.cs` — adapter's extraction of runtime executables and passthrough to builder

**Rationale:** Each type has single-responsibility focused logic; splitting tests by type keeps them cohesive. The orchestrator tests validate the domain contract (`ProcessResult` mapping, cancellation, failures); resolver/adapter tests validate their adaptation logic. Integration is covered by existing tests (monitor, runner tests) and the real-process capture tests.

**Alternatives considered:**
- One large orchestrator test file → rejected: mixes concerns (orchestrator mapping vs resolver logic vs builder passthrough)
- Integration tests only → rejected: loses fast unit-test feedback for orchestrator's error paths

## Open Questions

1. **ProcessLauncherCaptureOutputTests port strategy:** Should the ported tests drive the `WindowsProcessOutputRunner` directly (cleaner, tests the actual adapter), or drive it via the orchestrator's `CaptureOutputAsync` (tests the full production path)? Driving the runner directly is chosen in Decision 2; confirm during implementation.

2. **Test count validation:** Expected ~1240 tests after (from ~1223 before: -13 ProcessLauncherTests, +~25 orchestrator/resolver/adapter, +~5 ported capture). Actual count depends on granularity of new tests. Confirm final count in tasks.md gates.

3. **Coverage gaps:** `ProcessLauncherTests` includes some edge cases (empty arguments, special characters) that may not have orchestrator equivalents. During implementation, audit whether those scenarios are covered by builder/runner integration tests or need explicit orchestrator tests.
