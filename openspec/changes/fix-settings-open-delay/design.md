# Design: fix-settings-open-delay

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| Settings open path | `SettingsLauncher.ShowSettings()` → `SettingsWindow.ShowSettings()` runs `this.viewModel.Load()` synchronously **before** `this.Show()` | `src/CLIHub/Settings/SettingsLauncher.cs:26-34`, `src/CLIHub/Views/SettingsWindow.xaml.cs:38-62` |
| Settings load fan-out | `SettingsViewModel.Load()` calls `this.AgentsState.Load(draft.DefaultAgentId)` at the end of a long synchronous sequence | `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:1255,1330` |
| The regression loop | `AgentManagementViewModel.Load()` iterates all plugins; for **every** plugin declaring an Update command it calls `this.processMonitor.GetRunningAgents(plugin)` | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:172-210` (loop body 181-205, call 188-190) |
| Intent comment | "Availability is a Load()-time snapshot over the process state (the page load is the refresh trigger). The execution-time block itself stays in AgentManagementService." | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:185-186` |
| Per-call cost | `GetRunningAgents(plugin)` delegates to `GetRunningAgents()`, which calls `this.processInspector.GetRunningProcesses()` fresh **every time** | `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:94-101` and `:60-87` (inspector call at 66) |
| Enumeration cost | `WindowsAgentProcessInspector.GetRunningProcesses()` does `Process.GetProcesses()`, then reads `process.MainModule?.FileName` (skip on failure) and `process.StartTime` for **every** process; no caching anywhere in the chain | `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:28-98` (MainModule 48-57, StartTime 65-74) |
| Matching rule | `AgentProcessMatcher.IsMatch` compares `Path.GetFileNameWithoutExtension(processInstance.ExecutablePath)` with `Path.GetFileNameWithoutExtension(plugin.Commands.Launch.Executable)`, `OrdinalIgnoreCase`; plugins without a Launch command never match; matching is **name-only** (paths and command lines are irrelevant to the result) | `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:33-44` (esp. 40-43), remark 25-29 |
| First-match rule | A process is attributed to the **first** plugin in catalog order that matches; one match per process | `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:69-79` (`break; // One match per process`) |
| Other monitor consumers | `AgentManagementService.UpdateAsync` execution-time gate (1 pass per update click); `WindowActionCoordinator.CanExecuteUpdate` + `ExecuteUpdate` (1-2 passes; `CanExecute` re-runs on WPF `CommandManager.RequerySuggested`) | `src/CLIHub.Core/Agents/AgentManagementService.cs:76-83`; `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-161,163-182`; `src/CLIHub/ViewModels/Mvvm/RelayCommand.cs:25-29` |
| Async precedent in Load | Versions/availability already arrive after Load: `PopulateVersions()` runs via `RunAsync` fire-and-forget and cards render with a `"…"` placeholder first | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:197,243-267` |
| Failure behavior | Inspector/monitor exceptions are caught and logged; callers get an empty list (blocked state never shows, `CanUpdate` stays true) | `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:81-84`, `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:52-57,82-95` |
| Regression origin | Commit `b218067 fix(agents): detach plugin reads and gate card update availability` (0.9.9) added the `processMonitor` dependency and the per-plugin `GetRunningAgents(plugin)` call in `Load()`; before it, `canUpdate` came from the declared command only. Shipped as CHANGELOG 0.9.9 Changed: "an agent's update action is gated only by that agent's own running processes (`harden-agent-plugin-boundaries`, `agent-ui`)" | `git show b218067`; `CHANGELOG.md:11,25` |

Measured cost (reported by the orchestrator on the user's machine, 347 processes; to be re-measured
at the Stage 2 gate): `Process.GetProcesses()` 15 ms; `MainModule` read for every process +2451 ms;
`StartTime` +28 ms → one full pass ≈ 2.5 s; with ~6 update-declaring agents `Load()` blocks ≈ 15 s.
The cost is O(process count × update-declaring agents).

## Governing requirements (quoted)

- `openspec/specs/agent-ui/spec.md:219-236` — *Agent card action availability*: "The card SHALL
  present actions appropriate to the agent's declared capabilities and current state." Scenario
  **Update blocked by a running agent**: "WHEN a process matching the agent being updated is running /
  THEN 'Update' is unavailable and the card explains that the running agent blocks it". Scenario
  **Other running agents do not block an update**: "WHEN processes of other agents are running and the
  agent being updated has no running processes / THEN 'Update' is available".
- `openspec/specs/preferences-ui/spec.md:535-557` — *Agent card actions*: Scenario **Update blocked
  while running**: "WHEN the user chooses 'Update' and a process matching that agent is running / THEN
  the update does not start and the card reports that it is blocked". Scenario **Other running agents
  do not block the update**: "WHEN the user chooses 'Update' and only processes of other agents are
  running / THEN the update starts normally".
- `openspec/specs/preferences-ui/spec.md:9-15` — *Settings window access*: "WHEN the user chooses
  'Settings' from the tray menu / THEN the Settings window opens showing the current preferences".

Neither spec pins a wall-clock deadline or an ordering for when the blocked state must appear; the
`agent-ui` scenario is **state-based** ("a process … is running") while the `preferences-ui` scenario
is **action-based** ("the user chooses 'Update'"). This distinction drives D2 below.

## D1 — Single enumeration per Load (DECIDED: in scope, Stage 1)

Replace the per-plugin monitor calls with one call, grouped client-side:

```csharp
var anyDeclaresUpdate = plugins.Any(p => p.Commands?.Get(AgentCommandKind.Update) is not null);
var runningByPlugin = anyDeclaresUpdate
    ? this.processMonitor.GetRunningAgents().ToLookup(agent => agent.Plugin.Id)
    : null;
// per plugin: var runningAgents = runningByPlugin?.Contains(plugin.Id) == true ? runningByPlugin[plugin.Id].ToList() : [];
```

- **Semantic equivalence proof.** Today `GetRunningAgents(p)` = `GetRunningAgents().Where(a => a.Plugin.Id == p.Id)`
  (`AgentProcessMonitor.cs:98-100`), and `GetRunningAgents()` attributes each process to its
  first-matching plugin (`:69-79`). Grouping the single result by `Plugin.Id` yields exactly the same
  partition; each card sees the identical `RunningAgent` list, `CanUpdate`, `Message`, and
  `blockedNames` ordering. No observable change.
- **Zero-enumeration case preserved.** Today plugins without an Update command never trigger a call;
  the `anyDeclaresUpdate` guard keeps that property when the catalog has no update-declaring plugin.
- **Failure behavior.** One swallowed-exception log entry instead of N; results identical (empty).
- **Why ViewModel-side, not a new monitor API.** The monitor already exposes the needed primitive
  (`GetRunningAgents()`); a grouped-overload would add Core API surface for one caller, against the
  repository rule to prefer existing patterns.
- **Effect:** N passes → 1 pass (≈ 15 s → ≈ 2.5 s measured). Leaves one full ≈ 2.5 s pass on the UI
  thread — addressed by D3.

## D2 — Move the enumeration off the UI thread (DECIDED: out of scope, backlog)

Shape: `Load()` builds cards with `CanUpdate = declaresUpdate` (the pre-`b218067` state) and a
fire-and-forget `RunAsync` pass populates `CanUpdate`/`Message` when the enumeration completes —
exactly how `PopulateVersions()` already delivers versions seconds after load
(`AgentManagementViewModel.cs:243-267`), with `"…"` as the placeholder.

UX consequences, verified against the specs:

- Cards render immediately; for ≈ 2.5 s a card can show **Update available while the agent's process
  is running**. The `agent-ui` scenario "Update blocked by a running agent" is state-based — its
  literal reading ("WHEN a process … is running THEN 'Update' is unavailable") is violated during the
  transient, so a **MODIFIED `agent-ui` spec delta (scenario names preserved) would be required** to
  bless the arrival-latency. The `preferences-ui` scenario is action-based and stays satisfied: the
  execution-time gate in `AgentManagementService.UpdateAsync` (`:76-83`) blocks the actual update
  regardless of card state.
- The user can click Update during the transient and receive a blocked message after the fact — safe
  but surprising; a busy/placeholder treatment for the Update button would be needed to make this
  feel intentional, i.e. new UI states, new spec text.
- `SettingsWindow.ShowSettings()` would also need restructuring (`viewModel.Load()` before
  `this.Show()`, `SettingsWindow.xaml.cs:40-54`) — the window would open before load completes,
  changing the "opens showing the current preferences" experience for every page, far beyond the
  agents page.

Rejected for this change: D3 reaches the same latency goal with **no** observable change and no spec
delta. D2 remains the right shape only if the whole Settings load is ever made async; recorded in
`docs/backlog.md` with the spec-delta requirement and the scenario-name constraint.

## D3 — Candidate-name pre-filter in the inspector (DECIDED: recommended in scope, Stage 2)

**Semantics check (the decisive fact).** The matcher's comparison basis is
`GetFileNameWithoutExtension(ExecutablePath)` vs `GetFileNameWithoutExtension(plugin launch
executable)`, `OrdinalIgnoreCase` (`AgentProcessMatcher.cs:40-43`). A process can match only if its
executable's file name equals some descriptor's launch executable file name. `Process.GetProcesses()`
already carries `Process.ProcessName` — the image name without extension — at snapshot cost (≈ 15 ms
measured), before any `MainModule` access. Therefore: filter snapshot entries by
`candidateNames.Contains(process.ProcessName)` (ordinal, case-insensitive) and read
`MainModule.FileName`/`StartTime` only for candidates. The expensive reads drop from
O(all processes) to O(candidate processes), which is near-zero on a typical machine.

- **Candidate-set derivation.** Candidates = `GetFileNameWithoutExtension(plugin.Commands.Launch.Executable)`
  over **all** catalog plugins with a Launch command — the same descriptors the matcher uses. Deriving
  the set anywhere else (e.g. only update-declaring plugins) would be a semantic narrowing: the
  monitor's all-plugins matching (`AgentProcessMonitor.cs:67-79`) feeds `GetRunningAgents(plugin)` for
  any plugin, and grouping in D1 consumes all groups. Single-source the derivation next to the matcher
  so the filter and the match rule cannot drift apart.
- **Consumers verified (grep of `GetRunningProcesses|GetRunningAgents|HasRunningAgents`):**
  `AgentManagementService.UpdateAsync:76` (update gate), `WindowActionCoordinator:155,173` (launch
  window availability + execution double-check), `AgentManagementViewModel:189` (this regression),
  plus tests (`AgentProcessMonitorTests`, `AgentManagementServiceTests`,
  `AgentManagementViewModelTests`, `WindowsAgentProcessInspectorTests`,
  `LaunchWindowViewModelBuilder:441`). All consume monitor results; only the monitor talks to the
  inspector, so a port change is contained.
- **Processes skipped today stay skipped.** A process whose `MainModule` is inaccessible is skipped
  today (`WindowsAgentProcessInspector.cs:52-57`) and can never appear in results; with the
  pre-filter, non-candidates are skipped earlier — same output set. PID < 100 skip (`:41-44`) and the
  catch-and-skip error handling are preserved for candidates.
- **Assumption A1 (residual risk).** Safety requires `Process.ProcessName ==
  GetFileNameWithoutExtension(MainModule.FileName)` for every process with an accessible `MainModule`.
  Both derive from the process image, so they agree for ordinary executables; no counter-example is
  known in this repository or its tests. If any Windows mechanism reports a name that differs from the
  image file name, that process would be filtered out and a match the current code finds would be
  lost — weakening both the load-time display and the execution-time gate. Mitigations: A1 documented
  here, candidates derived from the matcher's own comparison basis, inspector tests updated to cover
  the filtered path, and a **manual live-equivalence gate** (enumerate full vs filtered on the dev
  machine and compare matched sets) before Stage 2 is called done.
- **Port shape.** Replace `GetRunningProcesses()` with
  `GetRunningProcesses(IReadOnlySet<string> candidateProcessNames)` (single method). A retained
  parameterless overload was rejected: Moq doubles that set up only the old method would silently
  return empty for the filtered call, hiding breakage. A monitor-side filter was rejected: the monitor
  receives `AgentProcessInstance`s only after the expensive reads have already happened
  (`AgentProcessMonitor.cs:66`), so it cannot avoid them; using `System.Diagnostics.Process` directly
  in the monitor would also bypass the Core/Core.Windows port boundary (project context: Windows
  integration stays behind ports declared in Core).
- **Effect:** ≈ 2.5 s → snapshot (≈ 15 ms) + a handful of candidate reads, expected well under
  100 ms. Also reduces the launch-window `CanExecuteUpdate` requery cost
  (`WindowActionCoordinator.cs:145-161` + `RelayCommand.cs:25-29`) from a full pass to the same cheap
  filtered pass, without touching that code's behavior.

**Recommendation (what goes where).** In scope: D1 (Stage 1) + D3 (Stage 2). Backlog: D2 (async
blocked-state, with its spec-delta requirement) and a monitor-level result cache (staleness would
affect the execution-time safety gate — revisit only with an explicit invalidation story). D3 exceeds
the user-approved minimal baseline; confirm at review (open question 1).

## Alternatives considered and rejected

- **Do nothing / accept 2.5 s after D1:** the orchestrator's constraint says this may still feel
  slow; the complaint is about perceived latency, not just the 6× multiplier.
- **`Process.GetProcessesByName(name)` per candidate in the monitor:** N snapshots instead of one;
  same A1 assumption; violates the port boundary (monitor must stay BCL-process-free). Cheaper than
  today but strictly worse than the single-snapshot set filter.
- **Parallelize `MainModule` reads in the inspector:** keeps O(all processes) `MainModule` opens,
  adds thread-pool pressure and ordering concerns inside a Windows adapter; the pre-filter removes
  the reads instead.
- **Cache inspector results with a TTL:** changes freshness of the execution-time safety gate
  (an agent started within the TTL would not block an update); rejected for this change, parked.

## Deviations

- None from the orchestrator's brief. D3 is presented as a separable Stage 2 rather than merged into
  Stage 1 so the approved minimal fix can ship alone if the user prefers.

## Open questions

1. Include Stage 2 (D3) in this change, or ship Stage 1 only and take D3 as an immediate follow-up?
   (Recommended: include; the latency goal is not met by Stage 1 alone.)
2. Port shape confirmation: replace the parameterless `GetRunningProcesses()` (recommended) vs add an
   overload (rejected above)?
3. Does `docs/architecture/processes.md` describe the inspector contract and need a one-paragraph
   update for the candidate-name parameter? (Check during implementation.)
4. CHANGELOG placement: a "Fixed" entry under a new heading vs an Unreleased section — follow the
   maintainer's release convention at implementation time.
