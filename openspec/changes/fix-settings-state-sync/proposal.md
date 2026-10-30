## Why

Three deferred Settings immediate-apply and summary-drift defects (documented in `docs/backlog.md` lines 402-417 under "Settings immediate-apply and summary drift") cause:

1. **C2 — Watched-directory apply failure is swallowed:** `TryAddWatchedDirectory` returns `true` after a failed `ApplyScanningSettings()`, clearing the validation error and text input while leaving the directory out of the list (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:465-510`). The user sees the input field cleared as if it succeeded, with no indication that the apply failed.

2. **C3 — AGENTS summary is stale after Detect:** `SettingsViewModel` mirrors agent counts (`AgentsInstalledCount`/`AgentsSupportedCount`/`AgentsSupportSummary` at `:101-102`, `:492-516`), refreshing them only in `Load` (`:633`, `RefreshAgentSummary` `:688-693`). When Detect updates `AgentsState`, the Project Scanning page summary cards keep pre-Detect counts (`:191-194` in `SettingsWindow.xaml`).

3. **C4 — Post-persistence scanner reload can roll back saved settings:** `SettingsApplicationService.Save` persists configuration, then calls `projectScanner.Reload()` inside the outer try block (`src/CLIHub/Settings/SettingsApplicationService.cs:512-525`). A reload exception enters the catch, which rolls back system settings although `config.json` already holds the new values. `ApplyScanning` has the same persist-then-reload shape (`:299-318`).

These are behavior corrections, not pure refactoring.

## What Changes

### C2 — Watched-directory apply failure reporting

**Current behavior (file:line evidence):**
- `TryAddWatchedDirectory` (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:465-510`) adds the row (`:498`), calls `ApplyScanningSettings()` (`:500`), removes the row on failure (`:506`), then **returns `true`** (`:509`).
- `AddWatchedDirectory` (`:441-463`) then clears `NewWatchedDirectoryText` (`:460`) and `ValidationError` (`:461`) on any `true` return.

**Fix:**
- Change line 509: return `false` when `ApplyScanningSettings()` fails (after removing the row).
- Do not clear `NewWatchedDirectoryText` or `ValidationError` when `TryAddWatchedDirectory` returns `false`.
- The apply error set through the callback must remain visible.
- Successful and duplicate-path behavior stays unchanged.

### C3 — AGENTS summary drift

**Current behavior (file:line evidence):**
- `SettingsViewModel` mirrors `AgentsInstalledCount`/`AgentsSupportedCount`/`AgentsSupportSummary` (`:492-516`).
- `RefreshAgentSummary` (`:688-693`) is called only in `Load` (`:633`).
- `AgentsState` updates during Detect but does not notify the view model, so the summary cards stay stale.

**Fix:**
- Expose `int SupportedAgentCount { get; }` on `IAgentManagementSection` (`src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:11-31`). The implementation already has it (`AgentManagementViewModel.cs:98`).
- Replace the mirrors (`agentsInstalledCount`/`agentsSupportedCount` backing fields) with pass-throughs to `AgentsState.InstalledAgentCount`/`AgentsState.SupportedAgentCount`.
- Subscribe to `AgentsState.PropertyChanged` in the `SettingsViewModel` constructor so `AgentsInstalledCount`/`AgentsSupportedCount`/`AgentsSupportSummary` re-raise when the section's counts change.
- Unsubscribe in `Dispose`.
- Remove `RefreshAgentSummary` and its call/raise block.
- Keep the public property names (`AgentsInstalledCount`/`AgentsSupportedCount`/`AgentsSupportSummary`) for XAML/API compatibility.

### C4 — Best-effort reload policy

**Current behavior (file:line evidence):**
- `SettingsApplicationService.Save` (`src/CLIHub/Settings/SettingsApplicationService.cs:449-526`):
  - Persists at `:512` (`this.preferencesStore.Update(...)`).
  - Calls `ApplyStartupUpdateCheck` (`:514`).
  - Calls `this.projectScanner.Reload()` inside the outer try (`:517`).
  - A reload exception enters the catch at `:520`, which rolls back system settings (`:523`) although the config write at `:512` already succeeded.
- `ApplyScanning` (`:298-320`) has the same persist-then-reload shape: persist at `:309`, reload at `:312`, and return failure after the write.

**Agreed policy: reload is best-effort after a successful write.**

**Fix:**
- Wrap the reload in its own try/catch at both sites (`Save` `:517` and `ApplyScanning` `:312`).
- Log a warning on failure ("Settings saved but the project scanner could not reload with the new configuration" or similar).
- Return success (`Applied` for `Save`, `Applied` for `ApplyScanning`) after a successful write, even if reload fails.
- The outer catch still rolls back for apply/persistence failures (before the write).
- Document the best-effort scope in an XML doc comment on the method or near the reload site.

## Capabilities

### Modified Capabilities

- `preferences-ui`: Settings immediate-apply now reports watched-directory apply failures, the agent summary reflects current section counts, and scanner reload failures do not roll back successfully saved settings.

## Impact

**Modified code:**
- `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` — return `false` on apply failure; leave error visible.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — subscribe to `AgentsState.PropertyChanged`, remove mirrors, expose pass-throughs.
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — add `int SupportedAgentCount { get; }`.
- `src/CLIHub/Settings/SettingsApplicationService.cs` — best-effort reload in `Save` and `ApplyScanning`.

**Behavior changes:**
- A failed watched-directory add now reports the error and leaves the input text and validation error visible (C2).
- The agent summary cards on the Project Scanning page update immediately when Detect completes (C3).
- A scanner reload failure after a successful settings save does not roll back the saved settings (C4).

**Test updates:**
- Add regression test: apply failure returns `false`, row absent, error kept (`tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`).
- Add test: section count change raises VM summary notifications; Detect updates displayed counts.
- Add tests: `Save` and `ApplyScanning` return success when `Reload` throws, persistence `Update` happened, system rollback not invoked (`tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs`).

**No breaking changes:** All existing public API signatures remain unchanged. The `IAgentManagementSection` addition is compatible (consumers read an existing property on the implementation).

## Non-Goals

- Do not fix the `ProcessRuntimePreferenceTarget` double write, the `Load()` notification rewrite, dead placeholder pages, or other backlog items.
- No unrelated refactoring, XAML restyling, or message changes beyond the agreed C2/C4 behavior.

## Dependencies

None. This change is independent and requires no other pending changes.
