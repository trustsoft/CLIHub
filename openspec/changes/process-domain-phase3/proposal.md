# Process Domain Consumer Migration (Phase 3)

## Problem

Phase 1+2 of Proposal 4 (commit a64422c) introduced the unified process domain model and `IProcessOrchestrator`, but consumers still depend on the old fragmented services:

- **AgentCommandService** — main agent command executor — depends on `ProcessLauncher`
- **AgentVersionService** — version retrieval — depends on `ProcessLauncher`
- **ProcessRuntimePreferenceTarget** — runtime preference tester — depends on `ProcessLauncher`
- **StartupPreferencesApplier** — startup preference validator — depends on `ProcessLauncher`
- **AgentProcessMonitor** — process monitoring — depends on `IAgentProcessInspector` + `AgentProcessMatcher`

These consumers still use:
- `ProcessLauncher` for command launching and output capture (with `SetRuntime`/`GetRuntime`)
- `IAgentProcessInspector` + `AgentProcessMatcher` for process finding
- `ProcessCaptureResult` result type

The orchestrator is fully implemented and tested but unused. This change completes the migration.

**Evidence:**
- `src/CLIHub.Core/Agents/AgentCommandService.cs:13-23` — injects `ProcessLauncher`
- `src/CLIHub.Core/Agents/AgentVersionService.cs:30-48` — injects `ProcessLauncher`
- `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:11-23` — injects `ProcessLauncher`
- `src/CLIHub/Startup/StartupPreferencesApplier.cs:12-33` — injects `ProcessLauncher`
- `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:14-35` — injects `IAgentProcessInspector` + `AgentProcessMatcher`

## Goal

Migrate all five consumers to use `IProcessOrchestrator`:

1. **AgentCommandService**
   - Replace `ProcessLauncher` with `IProcessOrchestrator`
   - Map `PluginCommand` → `LaunchRequest`
   - Use `LaunchInteractiveAsync` for interactive commands
   - Use `CaptureOutputAsync` for version commands
   - Map `ProcessResult` → `AgentCommandResult`

2. **AgentVersionService**
   - Replace `ProcessLauncher` with `IProcessOrchestrator`
   - Use `CaptureOutputAsync` for version probe
   - Map `ProcessResult` to version extraction

3. **ProcessRuntimePreferenceTarget**
   - Replace `ProcessLauncher.SetRuntime` with runtime store injection
   - Remove `ProcessLauncher` dependency (now a dead-end adapter)

4. **StartupPreferencesApplier**
   - Replace `ProcessLauncher.SetRuntime` with runtime store injection
   - Keep `IStartupService` dependency unchanged

5. **AgentProcessMonitor**
   - Replace `IAgentProcessInspector` + `AgentProcessMatcher` with `IProcessOrchestrator`
   - Use `FindRunningAgentsAsync` with `ProcessMatchCriteria`
   - Map `AgentProcess` → `RunningAgent`

After all migrations:
- Mark `ProcessLauncher` as `[Obsolete]` (removal deferred to Phase 4)
- Preserve exact behavior (no functional changes)

## What Changes

### Modified Files (5 consumers)

**AgentCommandService:**
- `src/CLIHub.Core/Agents/AgentCommandService.cs:13-23` — inject `IProcessOrchestrator` + `IPreferencesStore`
- `src/CLIHub.Core/Agents/AgentCommandService.cs:26-54` — `ExecuteAsync` uses orchestrator
- `src/CLIHub.Core/Agents/AgentCommandService.cs:56-74` — `RunVersionAsync` uses `CaptureOutputAsync`

**AgentVersionService:**
- `src/CLIHub.Core/Agents/AgentVersionService.cs:30-48` — inject `IProcessOrchestrator` + `IPreferencesStore`
- `src/CLIHub.Core/Agents/AgentVersionService.cs:79-104` — `ProbeAndCacheAsync` uses `CaptureOutputAsync`

**ProcessRuntimePreferenceTarget:**
- `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:9-24` — inject `IPreferencesStore` instead of `ProcessLauncher`
- `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:23` — `SetRuntime` writes to store

**StartupPreferencesApplier:**
- `src/CLIHub/Startup/StartupPreferencesApplier.cs:12-25` — inject `IPreferencesStore` instead of `ProcessLauncher`
- `src/CLIHub/Startup/StartupPreferencesApplier.cs:29-35` — `Apply` writes runtime to store

**AgentProcessMonitor:**
- `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:14-35` — inject `IProcessOrchestrator` instead of `IAgentProcessInspector` + `AgentProcessMatcher`
- `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:78-107` — `GetRunningAgents` uses `FindRunningAgentsAsync`

### Modified Tests (5 test files)

- `tests/CLIHub.Core.Tests/Agents/AgentCommandServiceTests.cs` — mock `IProcessOrchestrator`
- `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs` — mock `IProcessOrchestrator`
- `tests/CLIHub.Tests/Settings/ProcessRuntimePreferenceTargetTests.cs` — mock `IPreferencesStore`
- `tests/CLIHub.Tests/Startup/StartupPreferencesApplierTests.cs` — mock `IPreferencesStore`
- `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs` — mock `IProcessOrchestrator`

### Modified Infrastructure (1 file)

- `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:14` — add `[Obsolete("Use IProcessOrchestrator. Will be removed in Phase 4.")]`

## Impact

### Positive
- **Single dependency** — consumers depend on `IProcessOrchestrator` instead of 3-5 services
- **Simpler testing** — mock one interface instead of multiple collaborators
- **Consistent types** — `LaunchRequest`, `ProcessResult`, `AgentProcess` everywhere
- **Clear migration path** — `ProcessLauncher` marked obsolete; removal signaled

### Risk
- **Behavior preservation** — adapters must replicate exact current behavior
- **Runtime preference handling** — must preserve stateful runtime selection
- **Test coverage** — all existing consumer tests must pass unchanged

## Non-Goals

- **No removal** of `ProcessLauncher` — marked obsolete, removed in Phase 4
- **No new features** — orchestrator capabilities unchanged
- **No breaking changes** — all public APIs preserved
- **No UI changes** — runtime selection UI untouched
- **No orchestrator extension** — focus is consumer migration only

## Dependencies

- Depends on: Phase 1+2 (commit a64422c) — `IProcessOrchestrator` implementation
- Blocks: Phase 4 (optional) — `ProcessLauncher` removal
- Related: `docs/proposals/proposal-004-process-domain-model.md` (approved proposal)
