## Why

`ProcessLauncher` has been marked `[Obsolete]` with `DiagnosticId = "CLIHUB001"` since commit 37e3c46 (Phase 3 of the process domain unification). The application now uses `IProcessOrchestrator` in production; no live production code depends on `ProcessLauncher`. The obsolete type remains solely for backwards compatibility with legacy tests. 33 CLIHUB001 warnings remain, all in test code or DI registration.

Phase 4 removes the obsolete type and its DI registrations, and adds the direct orchestrator unit tests that were deferred during Phase 3. This completes the process domain refactor.

## What Changes

**A. Remove `ProcessLauncher` entirely:**
- Delete `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs` (obsolete since commit 37e3c46)
- Remove its DI registrations from:
  - `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs` line 57
  - Remove test-only workarounds from `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs` lines 27-31
- Handle legacy tests:
  - Delete `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs` (26 CLIHUB001 warnings) — its behavioral coverage moves to the new orchestrator tests
  - Delete or rewrite `tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs` (4 CLIHUB001 warnings) — its real-process integration value must be preserved
  - Rewrite `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` lines 137 and 177 (2 CLIHUB001 warnings) — remove `ProcessLauncher.BuildStartInfo` and construct the command line via `WindowsCommandLineBuilder` directly
  - Remove `ProcessLauncher` resolution assertions from `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` line 22 (1 CLIHUB001 warning)

**B. Add direct orchestrator unit tests** (replace coverage lost with `ProcessLauncherTests`):
- `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs` — covers:
  - `LaunchInteractiveAsync`: success mapping (exit code, duration recorded), runtime-unavailable → `ProcessResult.Failure`, runner exception → `Failure` with message, `OperationCanceledException` → `Cancelled`
  - `CaptureOutputAsync`: success with trimmed output + exit code, `TimedOut` → `ProcessResult.Timeout`, `InvalidOperationException` (non-started) → `Failure`, cancellation → `Cancelled`
  - Batched and single-plugin `FindRunningAgentsAsync` delegating to the finder
  - `ResolveRuntimeAsync` passthrough
- `tests/CLIHub.Core.Tests/Processes/RuntimeResolverTests.cs` — covers PowerShell/CommandPrompt defaults when locator returns null, GitBash/PowerShell7 null when not located, mapping when located
- `tests/CLIHub.Core.Tests/Processes/CommandLineBuilderAdapterTests.cs` — covers interactive and captured command building for each `RuntimeKind`, including GitBash and PowerShell7 executable passthrough
- Port or rewrite `ProcessLauncherCaptureOutputTests` to preserve real-process integration coverage (10s timeout, exit codes, cancellation)

## Capabilities

### Modified Capabilities

- None (pure internal refactor)

## Impact

**Deleted code:**
- `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs` (261 lines)
- `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs` (13 tests)
- DI registration line in `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs`

**Modified code:**
- `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs` — remove ProcessLauncher registration
- `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs` — remove test-only ProcessLauncher workaround
- `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` — remove ProcessLauncher resolution assertion
- `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` — rewrite two tests to use `WindowsCommandLineBuilder` directly
- `tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs` — rewrite to drive `IProcessOutputRunner` or orchestrator directly

**New code:**
- `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs` — comprehensive orchestrator unit tests
- `tests/CLIHub.Core.Tests/Processes/RuntimeResolverTests.cs` — runtime resolution logic tests
- `tests/CLIHub.Core.Tests/Processes/CommandLineBuilderAdapterTests.cs` — command line building tests

**Expected result:**
- CLIHUB001 warnings drop from 33 to 0
- Test count: ~1223 - 13 (deleted ProcessLauncherTests) + ~25 (new orchestrator/resolver/adapter tests) + ~5 (ported capture tests) = ~1240 tests
- All tests green; the application behaves identically

## Non-Goals

- Changing the orchestrator's public API or behavior
- Modifying production code beyond removing the obsolete type
- Changing the process runner contracts
