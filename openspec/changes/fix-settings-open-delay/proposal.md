# Fix the Settings-window open delay regression introduced in 0.9.9

## Why

Since 0.9.9 (commit `b218067`, `harden-agent-plugin-boundaries`), opening the Settings window stalls
the UI thread for many seconds before the window appears. The regression path is:

tray/launch → `SettingsLauncher.ShowSettings()` → `SettingsWindow.ShowSettings()` calls
`this.viewModel.Load()` synchronously before `this.Show()`
(`src/CLIHub/Views/SettingsWindow.xaml.cs:38-62`) → `SettingsViewModel.Load()` calls
`this.AgentsState.Load(...)` (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:1330`) →
`AgentManagementViewModel.Load()` calls `processMonitor.GetRunningAgents(plugin)` **once per
update-declaring plugin** (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:188-190`) → each
call runs a full system process enumeration
(`src/CLIHub.Core/Agents/AgentProcessMonitor.cs:94-101` → `:60-87`) where
`WindowsAgentProcessInspector.GetRunningProcesses()` reads `MainModule?.FileName` and `StartTime`
for **every** process on the machine
(`src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:28-98`).

Measurements reported by the orchestrator on the user's machine (347 processes): `Process.GetProcesses()`
≈ 15 ms, per-process `MainModule` reads ≈ +2451 ms, `StartTime` ≈ +28 ms — one full pass ≈ 2.5 s.
With ~6 update-declaring agents the Settings open blocks ≈ 15 s. The cost is O(process count × agents),
which is why the delay is huge even on a powerful PC. Pre-0.9.9, `Load()` computed `CanUpdate` from the
declared command only (see the `b218067` diff) and no enumeration ran on this path.

This violates the spirit of `preferences-ui` → *Settings window access* → "Open from the tray":
"WHEN the user chooses 'Settings' from the tray menu THEN the Settings window opens showing the current
preferences" — the window does not open until the blocking work finishes.

## What Changes

Two stages, each independently shippable:

1. **Stage 1 — single enumeration per Load (approved minimal fix).** `AgentManagementViewModel.Load()`
   calls `AgentProcessMonitor.GetRunningAgents()` **once** and groups the result by `Plugin.Id`
   client-side, instead of one full enumeration per update-declaring plugin. The `declaresUpdate`
   guard is kept so that when no plugin declares an Update command, no enumeration runs at all
   (preserving the current zero-enumeration case). Expected effect: ≈ 15 s → ≈ 2.5 s on the measured
   machine.
2. **Stage 2 — candidate-name pre-filter in the process inspector (recommended addition).**
   `IAgentProcessInspector.GetRunningProcesses()` takes a set of candidate process names derived from
   the same descriptors the matcher uses; `WindowsAgentProcessInspector` compares each snapshot
   process's `ProcessName` (free from `Process.GetProcesses()`) against the candidates and reads
   `MainModule.FileName` / `StartTime` only for candidates. `AgentProcessMatcher` matches purely on
   the executable's file name, case-insensitively
   (`src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:40-43`), so this is
   semantics-preserving under one documented assumption (see design, A1). Expected effect:
   ≈ 2.5 s → well under 100 ms, and it equally de-costs the launch-window Update availability check
   (`WindowActionCoordinator.CanExecuteUpdate`, `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-161`),
   which performs a full enumeration per WPF `CommandManager` requery
   (`src/CLIHub/ViewModels/Mvvm/RelayCommand.cs:25-29`).

Stage 2 goes beyond the user-approved minimal baseline; it is included on the orchestrator's
instruction to analyze the "still ≈ 2.5 s after Stage 1" concern and is recommended because it is the
only option that removes the perceptible delay **without** changing observable card behavior (no spec
delta needed). It can be dropped at review if the strictly minimal fix is preferred — see open
questions.

Moving the enumeration off the UI thread (async blocked-state) was analyzed and is **not** included;
see design D2 and the backlog entry it produces.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Both stages preserve the observable, synchronous Load-time semantics exactly:

- Stage 1 is a pure call-pattern change: per-plugin results are a partition of the single
  enumeration's results (first-match-per-process + filter by `Plugin.Id`,
  `AgentProcessMonitor.cs:69-79,98-100`), so every card receives the identical `CanUpdate`/`Message`
  state it receives today.
- Stage 2 keeps the same `AgentProcessInstance` set for every process whose image name can match a
  descriptor; processes skipped today (inaccessible `MainModule`) remain skipped.

Therefore the change opts out of specs (`skip_specs: true` in `.openspec.yaml`), following the
convention of e.g. `openspec/changes/archive/2026-09-28-extract-user32-interop/`. If Stage 2 is
rejected at review and an async variant is chosen instead, that variant **does** change observable
timing of the `agent-ui` blocked-state scenarios and would require a spec delta with preserved
scenario names — it must not be implemented under this `skip_specs` declaration.

## Impact

- Code:
  - `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` (Stage 1: single call + grouping)
  - `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs` (Stage 2: candidate-name
    parameter)
  - `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs` (Stage 2: `ProcessName` pre-filter)
  - `src/CLIHub.Core/Agents/AgentProcessMonitor.cs` (Stage 2: derive candidates from the catalog and
    pass them down)
  - Possibly `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs` (Stage 2: single-source
    the candidate-name derivation next to the matching rule)
- Tests: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`,
  `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs`,
  `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`,
  `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs`,
  `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` (mock signature updates in Stage 2).
- Docs: `CHANGELOG.md` (Fixed entry), `docs/backlog.md` (D2 async blocked-state + monitor-level
  cache entries), `docs/architecture/processes.md` only if the inspector contract is described there.
- Specs: none (see Capabilities).
- Risk: Stage 2 rests on assumption A1 (`Process.ProcessName` equals the image file name without
  extension for every process with an accessible `MainModule`); a violation would drop matches the
  current code finds. Mitigations: candidates derived from the same descriptors the matcher uses,
  updated inspector tests, and a manual live-equivalence check gate (tasks 2.4). The execution-time
  safety gate in `AgentManagementService.UpdateAsync`
  (`src/CLIHub.Core/Agents/AgentManagementService.cs:76-83`) uses the same enumeration, so it shares
  the assumption — this is why A1 verification is a hard gate, not a nice-to-have.

## Non-goals

- No asynchronous / off-UI-thread blocked-state population (design D2) — deferred to backlog.
- No result caching or TTL for running-agent lookups — a cache would introduce staleness into the
  execution-time safety gate and change observable behavior; parked in backlog with that risk
  recorded.
- No change to matching semantics (path- or command-line-based matching, runtime-host `node.exe`
  shims) — already parked in `docs/backlog.md` ("Runtime-hosted agent blocking (Option B)").
- No behavioral edits to `WindowActionCoordinator` or the launch window; they only benefit from the
  cheaper enumeration.
- No UI/card layout changes.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change).
- Reverts, without contradicting, the cost profile of archived
  `2026-10-20-harden-agent-plugin-boundaries` (introduced the per-plugin Load loop) and reuses the
  per-agent overloads from archived `2026-10-19-per-agent-update-blocking`, which stay unchanged.
- Respects the `agent-ui` → *Agent card action availability* and `preferences-ui` → *Agent card
  actions* requirements by keeping their behavior identical (quoted in design).
