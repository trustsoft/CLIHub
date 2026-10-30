## Stage 1: C2 — Watched-directory apply failure reporting

- [x] 1.1 Read `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:465-510` and verify the current `TryAddWatchedDirectory` logic
- [x] 1.2 Change line 509: replace `return true;` with `return false;` (return false when apply fails after removing the row)
- [x] 1.3 Verify `AddWatchedDirectory` (`:441-463`) and `AddWatchedDirectoryFromPicker` (existing callers) do not clear input or error when `TryAddWatchedDirectory` returns `false`
- [x] 1.4 Build the solution with 0 errors/warnings: `dotnet build CLIHub.sln`
- [x] 1.5 Add regression test `AddWatchedDirectory_ApplyFailure_ReportsErrorAndKeepsInput` to `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` using the existing `Fixture`:
  - Configure the fixture's `IPreferencesStore.Update` to throw (the apply failure path); keep `Load` working
  - Set `Scanning.NewWatchedDirectoryText` to an existing absolute directory and execute `AddWatchedDirectoryCommand`
  - Assert: `Scanning.WatchedDirectories` is empty (the row was removed), `ViewModel.ValidationError` contains the apply error, and `Scanning.NewWatchedDirectoryText` still equals the path
  - Do **not** try to mock `WatchedDirectoryEditor.ApplyScanningSettings` (it is not virtual); drive the failure through `SettingsApplicationService.ApplyScanning` via the mock store.
- [x] 1.6 Run all tests: `dotnet test CLIHub.sln` — verify 0 failures and the new test passes

## Stage 2: C3 — AGENTS summary reflects current counts

- [x] 2.1 Read `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:11-31` and add `int SupportedAgentCount { get; }` after line 21 (InstalledAgentCount)
- [x] 2.2 Verify `AgentManagementViewModel.cs` already implements `SupportedAgentCount` (line 98) — no implementation change needed
- [x] 2.3 Read `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:101-102`, `:492-516`, `:633`, `:688-693`
- [x] 2.4 Remove private fields `agentsInstalledCount` and `agentsSupportedCount` at lines 101-102
- [x] 2.5 Replace `AgentsInstalledCount`, `AgentsSupportedCount`, `AgentsSupportSummary` properties (`:492-516`) with pass-throughs:
  ```csharp
  public int AgentsInstalledCount => this.AgentsState.InstalledAgentCount;
  public int AgentsSupportedCount => this.AgentsState.SupportedAgentCount;
  public string AgentsSupportSummary => $"{this.AgentsSupportedCount} supported";
  ```
- [x] 2.6 In the `SettingsViewModel` constructor (after `this.AgentsState = agentsState;`), subscribe to `this.AgentsState.PropertyChanged`:
  - Handle `InstalledAgentCount` and `SupportedAgentCount` changes
  - Raise `PropertyChanged` for `AgentsInstalledCount`, `AgentsSupportedCount`, and `AgentsSupportSummary`
- [x] 2.7 In `SettingsViewModel.Dispose()`, unsubscribe from `AgentsState.PropertyChanged` before calling `this.AgentsState.Dispose()`
- [x] 2.8 Remove `RefreshAgentSummary()` method (`:688-693`) and its call in `Load()` (`:633`)
- [x] 2.9 Build the solution: `dotnet build CLIHub.sln` — verify 0 errors/warnings
- [x] 2.10 Review all `Mock<IAgentManagementSection>` setups in tests (e.g., `SettingsViewModelApplicationTests.cs`, `ServiceRegistrationTests.cs`) and add `SupportedAgentCount` setup where needed
- [x] 2.11 Add test `AgentSummary_DetectUpdatesInstalledCount_RaisesPropertyChanged` to `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`:
  - Arrange: create `SettingsViewModel` with mocked `IAgentManagementSection`
  - Act: raise `PropertyChanged` on the mock for `InstalledAgentCount` and `SupportedAgentCount`
  - Assert: `SettingsViewModel` raised `PropertyChanged` for `AgentsInstalledCount`, `AgentsSupportedCount`, `AgentsSupportSummary`; properties return section's counts
- [x] 2.12 Run all tests: `dotnet test CLIHub.sln` — verify 0 failures

## Stage 3: C4 — Best-effort reload after successful write

- [x] 3.1 Read `src/CLIHub/Settings/SettingsApplicationService.cs:449-526` (`Save`) and `:298-320` (`ApplyScanning`)
- [x] 3.2 In `Save` method (`:449-526`), wrap the reload call at line 517 in a try/catch:
  ```csharp
  // Reconfigure watching and rescan with the newly saved scanning settings.
  // The scanner reload is best-effort; a reload failure does not cause rollback after a successful write.
  try
  {
      this.projectScanner.Reload();
  }
  catch (Exception reloadEx)
  {
      this.logger.LogWarning(reloadEx, "Settings saved but the project scanner could not reload with the new configuration");
  }
  ```
- [x] 3.3 In `ApplyScanning` method (`:298-320`), wrap the reload call at line 312 in a try/catch with the same warning message
- [x] 3.4 Verify both methods return `SettingsApplicationResult.Applied` after the try/catch (not inside the outer catch, which still rolls back for pre-persist failures)
- [x] 3.5 Build the solution: `dotnet build CLIHub.sln` — verify 0 errors/warnings
- [x] 3.6 Add test `Save_ReloadFails_ReturnsSuccessAfterPersist` to `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs`:
  - Mock `projectScanner.Reload()` to throw
  - Call `Save(validDraft)`
  - Assert: `result.Success` is true, `preferencesStore.Update` called once, `Rollback` not called, logger recorded warning
- [x] 3.7 Add test `ApplyScanning_ReloadFails_ReturnsSuccessAfterPersist`:
  - Mock `projectScanner.Reload()` to throw
  - Call `ApplyScanning(validInput)`
  - Assert: `result.Success` is true, `preferencesStore.Update` called once, logger recorded warning
- [x] 3.8 Add test `Save_ApplyFailure_RollsBackWithoutPersist` (if not already covered):
  - Mock an apply failure (e.g., hotkey) before persist
  - Call `Save(draft)`
  - Assert: `result.Success` is false, `preferencesStore.Update` not called, `Rollback` called once
- [x] 3.9 Run all tests: `dotnet test CLIHub.sln` — verify 0 failures

## Stage 4: Final verification and validation

- [x] 4.1 Build the full solution: `dotnet build CLIHub.sln` — verify 0 errors/warnings
- [x] 4.2 Run all tests: `dotnet test CLIHub.sln` — verify all tests pass (no failures, no skips for new tests)
- [x] 4.3 Run OpenSpec validation: `openspec validate --all` — verify all specs pass
- [x] 4.4 Manual smoke (user-confirmed 2026-10-30): open Settings → Project Scanning, add and remove a watched directory (success path), change depth/discovery and ignored patterns (immediate apply); open CLI Agents → run Detect → return to Project Scanning and confirm the AGENTS summary counts update without reopening; change a preference and Save; reopen Settings and confirm persistence.
- [x] 4.5 Update `graphify-out/` if required by repository tooling: `graphify update .`

**Do NOT commit.** Report the result and the validation output, then ask the user whether to archive and commit.
