## Context

Three immediate-apply and summary-drift defects were deferred in `docs/backlog.md` (lines 402-417):
- C2: watched-directory apply failure is swallowed.
- C3: agent summary is stale after Detect.
- C4: post-persistence scanner reload can roll back saved settings.

See `proposal.md` for the why and what. This document records the verified current state, the agreed fixes, test plan, and open questions.

## Verified Current State (file:line)

### C2 — Watched-directory apply failure

**`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`:**
- `:465-510` — `TryAddWatchedDirectory` method.
- `:498` — `this.WatchedDirectories.Add(row);`
- `:500-503` — `if (this.ApplyScanningSettings()) { return true; }`
- `:505-506` — `this.WatchedDirectories.Remove(row);` (after failed apply)
- **`:509` — `return true;`** — the bug: always returns true, even after removing the row due to apply failure.

**`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:441-463` — `AddWatchedDirectory`:**
- `:455-462` — on `TryAddWatchedDirectory(...)` success, clears `NewWatchedDirectoryText` (`:460`) and calls `setValidationError(null)` (`:461`).
- The success path runs even when apply failed (because `:509` returns `true`).

**Consequence:** A failed apply removes the row but clears the error and input text, so the user sees no indication that the add failed.

### C3 — AGENTS summary drift

**`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`:**
- `:101-102` — private fields `agentsInstalledCount` and `agentsSupportedCount`.
- `:492-516` — `AgentsInstalledCount`, `AgentsSupportedCount`, `AgentsSupportSummary` properties (mirrors).
- `:633` — `Load()` calls `this.RefreshAgentSummary();`
- `:688-693` — `RefreshAgentSummary()` reads `this.AgentsState.Cards` and sets the mirrors.
- `AgentsState` updates during Detect (`AgentManagementViewModel.cs:378` raises `PropertyChanged` for `InstalledAgentCount`/`SupportedAgentCount`), but `SettingsViewModel` does not subscribe, so the mirrors stay stale.

**`src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs`:**
- `:21` — `int InstalledAgentCount { get; }`
- `:26` — `string AvailabilitySummary { get; }`
- No `SupportedAgentCount` property on the interface, although the implementation has it (`AgentManagementViewModel.cs:98`).

**Consequence:** The Project Scanning page summary cards (bound to `AgentsInstalledCount`/`AgentsSupportedCount`) show pre-Detect counts until the Settings window is closed and reopened.

### C4 — Post-persistence scanner reload

**`src/CLIHub/Settings/SettingsApplicationService.cs`:**
- `:449-526` — `Save(SettingsDraft draft)` method.
- `:512` — `this.preferencesStore.Update(preferences => WritePreferences(preferences, draft));` — the persist step.
- `:514` — `this.applier.ApplyStartupUpdateCheck(draft.CheckForUpdatesOnStartup);`
- `:517` — `this.projectScanner.Reload();` — inside the outer try.
- `:520-525` — the catch block logs and rolls back (`:523` `this.Rollback(items);`) without distinguishing between pre-persist and post-persist failures.

**`src/CLIHub/Settings/SettingsApplicationService.cs`:**
- `:298-320` — `ApplyScanning(ScanningSettingsInput input)` method.
- `:309` — `this.preferencesStore.Update(preferences => WriteScanning(preferences, normalized));` — the persist step.
- `:312` — `this.projectScanner.Reload();` — inside the outer try.
- `:315-318` — the catch logs and returns failure after the write.

**Consequence:** A reload exception after a successful write rolls back system settings that no longer match the persisted `config.json`, or returns failure while the config holds the new values.

## Decisions

### Decision 1: C2 — Return false on apply failure

**Chosen:** Change line 509 to `return false;` when `ApplyScanningSettings()` fails. The apply error set by the callback remains in `WatchedDirectoryError`, and `NewWatchedDirectoryText` is not cleared.

**Rationale:** The existing pattern (`:465-488`) returns `false` for validation failures and keeps the error visible. Apply failure is the same failure class. The duplicate-path path at `:491-507` returns `true` (no error) and clears input because the list already contains the directory — that behavior stays unchanged.

**Alternatives considered:**
- Always clear input even on failure: rejected — hides the failure from the user.
- Add a separate error property: rejected — `WatchedDirectoryError` already exists and the apply callback writes it.

### Decision 2: C3 — Expose SupportedAgentCount and subscribe

**Chosen:**
1. Add `int SupportedAgentCount { get; }` to `IAgentManagementSection` (`src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs`).
2. Remove the backing fields `agentsInstalledCount` and `agentsSupportedCount` from `SettingsViewModel`.
3. Replace the properties with pass-throughs:
   ```csharp
   public int AgentsInstalledCount => this.AgentsState.InstalledAgentCount;
   public int AgentsSupportedCount => this.AgentsState.SupportedAgentCount;
   public string AgentsSupportSummary => $"{this.AgentsSupportedCount} supported";
   ```
4. Subscribe to `this.AgentsState.PropertyChanged` in the `SettingsViewModel` constructor (after assigning `this.AgentsState`), handling `InstalledAgentCount` and `SupportedAgentCount` changes by raising `PropertyChanged` for the three VM properties.
5. Unsubscribe in `Dispose()` (before calling `this.AgentsState.Dispose()`).
6. Remove `RefreshAgentSummary()` and its call in `Load()`.

**Rationale:**
- The implementation (`AgentManagementViewModel`) already has `SupportedAgentCount` (`:98`), so adding it to the interface is safe.
- Pass-throughs eliminate the mirrors and their staleness.
- The subscription ensures the summary cards update when Detect completes.
- Unsubscribing in `Dispose` avoids holding a stale reference.

**Alternatives considered:**
- Keep the mirrors and subscribe: rejected — adds redundant state and synchronization logic.
- Expose `INotifyPropertyChanged` on the interface: rejected — pass-throughs are simpler and the interface already has the counts.

### Decision 3: C4 — Best-effort reload after successful write

**Chosen:**
1. Wrap the reload in its own try/catch at both sites:
   - `Save` line 517
   - `ApplyScanning` line 312
2. Log a warning on reload failure: "Settings saved but the project scanner could not reload with the new configuration" (or similar).
3. Return success (`Applied`) after a successful write, even if reload fails.
4. The outer catch still rolls back for apply/persistence failures (before the write).
5. Add an XML doc comment near the reload or on the method: "The scanner reload is best-effort; a reload failure after a successful write does not cause rollback."

**Rationale:**
- Reload is a cache refresh, not a persistence step. A failure should not undo a successful write.
- The user's settings are saved and will take effect on the next scan or application restart.
- Logging the reload failure ensures visibility without blocking the save.

**Alternatives considered:**
- Roll back on reload failure: rejected — the write already happened; rolling back system settings creates inconsistency.
- Reload before persist: rejected — the reload should use the new settings, not the old ones.
- Return failure on reload failure: rejected — the settings are already saved and applied; the reload is a cache sync.

## Test Plan

### C2 — Watched-directory apply failure

**File:** `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`

**New test:** `AddWatchedDirectory_ApplyFailure_ReportsErrorAndKeepsInput`
- **Arrange:** Mock `ApplyScanningSettings` to return `false` (simulate persist/reload failure).
- **Act:** Set `NewWatchedDirectoryText` to a valid directory and execute `AddWatchedDirectoryCommand`.
- **Assert:**
  - `TryAddWatchedDirectory` returned `false`.
  - `WatchedDirectories` does not contain the directory (the row was removed).
  - `WatchedDirectoryError` contains the apply error (not cleared).
  - `NewWatchedDirectoryText` still contains the directory (not cleared).

**Existing tests to preserve:**
- `AddWatchedDirectory_ExistingAbsolutePath_AddsRootAndClearsInput` (`:308-323`) — success path unchanged.
- `AddWatchedDirectory_MissingFolder_ReportsError` (`:324-337`) — validation failure unchanged.
- `AddWatchedDirectory_AppliesImmediatelyAndReloadsScanner` (`:389-410`) — success path persists and reloads.

### C3 — AGENTS summary reflects current counts

**File:** `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` or a new focused test file.

**New test:** `AgentSummary_DetectUpdatesInstalledCount_RaisesPropertyChanged`
- **Arrange:** Create a `SettingsViewModel` with a mocked `IAgentManagementSection`.
- **Act:** Simulate a Detect completion by raising `PropertyChanged` on the mock for `InstalledAgentCount` and `SupportedAgentCount`.
- **Assert:**
  - `SettingsViewModel` raised `PropertyChanged` for `AgentsInstalledCount`, `AgentsSupportedCount`, and `AgentsSupportSummary`.
  - The properties return the section's current counts.

**Existing tests to preserve:**
- Any tests that construct `Mock<IAgentManagementSection>` may need setup for `SupportedAgentCount` (the new interface member). Ensure mocks do not break.
- `ServiceRegistrationTests.cs` — if it asserts interface members, add the new property to the assertion.

### C4 — Best-effort reload

**File:** `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs`

**New tests:**

1. `Save_ReloadFails_ReturnsSuccessAfterPersist`
   - **Arrange:** Mock `projectScanner.Reload()` to throw an exception.
   - **Act:** Call `Save(validDraft)`.
   - **Assert:**
     - `result.Success` is `true`.
     - `preferencesStore.Update` was called once (persistence happened).
     - `Rollback` was not called (no system rollback).
     - Logger recorded a warning about the reload failure.

2. `ApplyScanning_ReloadFails_ReturnsSuccessAfterPersist`
   - **Arrange:** Mock `projectScanner.Reload()` to throw an exception.
   - **Act:** Call `ApplyScanning(validInput)`.
   - **Assert:**
     - `result.Success` is `true`.
     - `preferencesStore.Update` was called once.
     - Logger recorded a warning.

3. `Save_ApplyFailure_RollsBackWithoutPersist`
   - **Arrange:** Mock an apply failure (e.g., hotkey) before the persist step.
   - **Act:** Call `Save(draft)`.
   - **Assert:**
     - `result.Success` is `false`.
     - `preferencesStore.Update` was not called.
     - `Rollback` was called once (system rollback for apply failure).

**Existing tests to preserve:**
- `Save_ValidDraft_AppliesThenPersistsAndReportsSuccess` (`:33-80`) — success path with reload unchanged.
- `Save_HotkeyFailure_RollsBackSystemChangesAndDoesNotPersist` (`:82-113`) — apply failure before persist unchanged.

## Risks and Mitigations

**Risk 1:** Mocking `IAgentManagementSection.SupportedAgentCount` breaks existing tests.
- **Mitigation:** Review all mock setups and add the property where needed. The implementation already has it, so the change is backward-compatible.

**Risk 2:** Reload failure after persist is not immediately visible to the user.
- **Mitigation:** Log a warning with clear wording. The settings are saved and will take effect on the next scan or restart. Consider a future UI notification if this becomes a common case.

**Risk 3:** C2 changes break the duplicate-path behavior.
- **Mitigation:** The duplicate-path block (`:491-507`) returns before the apply, so it is unaffected. Add a test to confirm.

## Alternatives Rejected

### C2 — Clear input but keep error

**Considered:** Clear `NewWatchedDirectoryText` but keep `WatchedDirectoryError` visible.

**Rejected:** Clearing the input suggests success. The error alone is ambiguous without the text.

### C3 — Poll AgentsState instead of subscribing

**Considered:** Refresh the summary on a timer or when the page is activated.

**Rejected:** Polling is inefficient and error-prone. Subscription is the MVVM pattern.

### C4 — Reload before persist

**Considered:** Reload the scanner before persisting settings, so a reload failure blocks the save.

**Rejected:** The reload should use the new settings, not the old ones. Reloading before persist would rescan with stale configuration.

## Open Questions

1. **Should C4 surface a user-visible notification for reload failures?**
   - Current: only a log warning.
   - Proposal: Add a toast or status line ("Settings saved; scanner reload failed, will retry on restart").
   - **Decision for this change:** Log only. Notification is a future enhancement if this proves common.

2. **Should the C2 error message be more specific?**
   - Current: the apply callback writes a generic "Could not apply scanning settings: <message>".
   - Proposal: Prefix with "Could not add watched directory:".
   - **Decision for this change:** Use the existing error format. The context (input field and "Add" action) already clarifies the operation.

3. **Should `IAgentManagementSection.SupportedAgentCount` be optional (nullable) for backward compatibility?**
   - Current: adding a required property to an interface.
   - Proposal: Make it nullable or provide a default implementation.
   - **Decision for this change:** Required property. The implementation already has it; no external consumers exist.

## Deviations from Brief

None. The brief accurately described the current state and the agreed fixes. All file:line references were verified.
