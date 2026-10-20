# Tasks: fix-settings-open-delay

## 1. Stage 1 — single enumeration per Load (D1)

- [x] 1.1 In `AgentManagementViewModel.Load()` (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:172-210`):
      collect plugins first; if none declares an Update command, skip the monitor call entirely;
      otherwise call `this.processMonitor.GetRunningAgents()` once and build
      `ILookup<string, RunningAgent>` keyed by `Plugin.Id`; per card, take the plugin's group (empty
      list when absent). Keep `CanUpdate`, `Message`, and `blockedNames` construction byte-identical
      to today, and keep the Load()-time-snapshot comment.
- [x] 1.2 Add a test in `AgentManagementViewModelTests` proving exactly one
      `GetRunningProcesses()` call per `Load` with multiple update-declaring plugins (Moq
      `Verify(..., Times.Once)`), and a test that no call happens when no plugin declares Update.
- [x] 1.3 Confirm existing blocked-state tests stay green unchanged:
      `Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock`,
      `Load_OtherAgentsProcessRunning_KeepsUpdateAvailableWithoutMessage`
      (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:298-338`).
- [x] 1.4 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures.

## 2. Stage 2 — candidate-name pre-filter (D3) — skip section 2 and 3 if dropped at review

- [x] 2.1 Single-source the candidate derivation: add a method (on `AgentProcessMatcher` or beside
      it) returning `IReadOnlySet<string>` of `Path.GetFileNameWithoutExtension(Commands.Launch.Executable)`
      for all plugins with a Launch command, ordinal case-insensitive comparer — the matcher's exact
      comparison basis (`src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:40-43`).
      Unit-test it against the matcher (every name a matcher can accept passes the filter).
- [x] 2.2 Change `IAgentProcessInspector.GetRunningProcesses()` to take the candidate-name set
      (`src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs:15`); update
      `AgentProcessMonitor.GetRunningAgents()` to derive candidates from the catalog and pass them
      (`src/CLIHub.Core/Agents/AgentProcessMonitor.cs:66`). Update XML docs to state the contract:
      only processes whose `ProcessName` is in the set are returned.
- [x] 2.3 Update `WindowsAgentProcessInspector.GetRunningProcesses()`
      (`src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:28-98`): keep `Process.GetProcesses()`
      and the PID < 100 skip; add the `ProcessName` candidate check **before** any `MainModule`/
      `StartTime` access; keep the existing catch-and-skip behavior and `Dispose` for candidates.
- [x] 2.4 Update inspector tests (`tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs`)
      for the new signature: candidate names common on any machine (e.g. `explorer`, `svchost`)
      return non-empty with valid paths/times; an empty/non-matching candidate set returns empty.
- [x] 2.5 Update monitor/service/view-model test doubles and the
      `LaunchWindowViewModelBuilder` mock to the new signature
      (`AgentProcessMonitorTests.cs`, `AgentManagementServiceTests.cs:31`,
      `AgentManagementViewModelTests.cs:50,74`, `LaunchWindowViewModelBuilder.cs:441`); keep the
      1.2 single-call test intact.
- [x] 2.6 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures.
- [x] 2.7 Gate (manual, assumption A1): user-confirmed 2026-10-20 — Settings opens without the
      regression delay and blocked-state cards behave as before. Supplementary harness evidence
      gathered during implementation (211 accessible processes, 0 A1 violations, full-vs-filtered
      matched sets identical 54 = 54, full pass 851 ms vs filtered 20 ms); harness deleted.

## 3. Docs

- [x] 3.1 `CHANGELOG.md`: Fixed entry — Settings open delay caused by per-agent full process
      enumeration on load; single enumeration + name pre-filter; no behavior change.
- [x] 3.2 `docs/backlog.md`: add the D2 async blocked-state entry (evidence, spec-delta requirement
      with preserved scenario names, trigger: Settings open still perceived slow after Stage 2 or a
      request for async Settings load) and the monitor-cache entry with the execution-gate staleness
      risk. Update `docs/architecture/processes.md` only if it names the inspector signature.
- [x] 3.3 Remove any temporary measurement harness from 2.7; confirm `git status` shows only files
      belonging to this change.

## 4. Verification

- [x] 4.1 `openspec validate --all` passes.
- [x] 4.2 `graphify update .` to refresh the knowledge graph after source edits.
- [x] 4.3 Live check passed 2026-10-20 (user smoke): Settings opens from the tray without a
      perceptible pause; blocked cards still show "Update blocked while the agent is running: …";
      Update on a non-running agent still starts and is still blocked at execution time when its own
      agent is running.
