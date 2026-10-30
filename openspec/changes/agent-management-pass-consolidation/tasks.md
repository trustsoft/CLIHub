# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive.

## Stage 1: Add shared primitives and focused tests

**Goal:** Add `RestartablePass` and its focused unit tests. No migration yet; existing code is unchanged.

- [x] 1.1 Create `src/CLIHub/ViewModels/Shared/RestartablePass.cs` with namespace `CLIHub.ViewModels`. Implement the `RestartablePass` class and `PassLease` record struct per the design API. Both types are `internal`; `CLIHub.Tests` already has `InternalsVisibleTo` (`src/CLIHub/CLIHub.csproj:25`), so the focused unit tests can reach them. Add `using` directives in the file's configured ordering (check adjacent Shared files for the pattern).

- [x] 1.2 Add `tests/CLIHub.Tests/ViewModels/Shared/RestartablePassTests.cs`. Write focused unit tests:
  - `Begin_ReturnsLeaseWithIncrementedGeneration`: first `Begin()` returns generation 1, second returns generation 2.
  - `Begin_CancelsPreviousToken`: call `Begin()` twice; the first lease's `RefreshToken` reports `IsCancellationRequested == true` after the second `Begin()`.
  - `IsCurrent_ReturnsTrueWhenGenerationMatchesAndTokenNotCancelled`: `Begin()`, then `IsCurrent(lease.Generation, lease.RefreshToken)` returns true.
  - `IsCurrent_ReturnsFalseWhenSuperseded`: first `Begin()`, second `Begin()`, then `IsCurrent(firstLease.Generation, firstLease.RefreshToken)` returns false.
  - `IsCurrent_ReturnsFalseWhenTokenCancelled`: `Begin()`, cancel the token externally via a linked CTS, then `IsCurrent(lease.Generation, cancelledToken)` returns false.
  - `Dispose_InvalidatesGenerationAndCancelsToken`: `Begin()`, `Dispose()`, then `IsCurrent(lease.Generation, lease.RefreshToken)` returns false.
  - `Dispose_IsIdempotent`: `Dispose()` twice does not throw.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — all tests green, including the new `RestartablePassTests`.

## Stage 2: Migrate `AgentPaneController` to `RestartablePass`

**Goal:** `AgentPaneController` uses `RestartablePass` instead of its own CTS/generation fields. No other changes.

- [x] 2.1 `src/CLIHub/ViewModels/Agents/AgentPaneController.cs` already declares `namespace CLIHub.ViewModels;`, so no new `using` is needed; confirm this before editing.

- [x] 2.2 Same file: replace the fields `versionPopulationCts` and `versionPopulationGeneration` (lines 23-24) with a single field `private readonly RestartablePass versionPopulationPass = new();`.

- [x] 2.3 Same file, `Refresh` method (lines 92-96): replace the cancel/dispose/create/bump/capture sequence with:
  ```csharp
  var lease = this.versionPopulationPass.Begin();
  ```
  Then pass `lease.Generation` and `lease.RefreshToken` to the `RunAsync` call (instead of `generation` and `refreshCancellationToken`).

- [x] 2.4 Same file, `PopulateVersionsAsync` method (lines 164-193): replace the guard `!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration` (line 180) with `this.versionPopulationPass.IsCurrent(generation, cancellationToken)`.

- [x] 2.5 Same file, `Dispose` method (lines 149-162): replace the generation-increment + cancel/dispose/null sequence (lines 157-160) with `this.versionPopulationPass.Dispose();`.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — all tests green, including existing `AgentPaneControllerTests`.

## Stage 3: Migrate `AgentManagementViewModel` passes to `RestartablePass`

**Goal:** Both `PopulateVersions`/`PopulateBlockedState` use `RestartablePass` instead of their own CTS/generation fields. No busy-state changes yet.

- [x] 3.1 `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` already declares `namespace CLIHub.ViewModels;`, so no new `using` is needed; confirm this before editing.

- [x] 3.2 Same file: locate the fields `versionPopulationCts`, `versionPopulationGeneration`, `blockedStateCts`, `blockedStateGeneration` (referenced in `PopulateVersions`, `PopulateBlockedState`, and `Dispose`). Replace them with:
  ```csharp
  private readonly RestartablePass versionPopulationPass = new();
  private readonly RestartablePass blockedStatePass = new();
  ```

- [x] 3.3 Same file, `PopulateVersions` method (lines 413-426): replace the cancel/dispose/create/bump/capture sequence (lines 415-419) with:
  ```csharp
  var lease = this.versionPopulationPass.Begin();
  ```
  Then pass `lease.Generation` and `lease.RefreshToken` to the `RunAsync` call.

- [x] 3.4 Same file, `PopulateVersionsAsync` method (lines 428-482): replace every guard `!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration` (lines 447, 456, 464, 470) with `this.versionPopulationPass.IsCurrent(generation, cancellationToken)`.

- [x] 3.5 Same file, `PopulateBlockedState` method (lines 484-497): replace the cancel/dispose/create/bump/capture sequence (lines 486-490) with:
  ```csharp
  var lease = this.blockedStatePass.Begin();
  ```
  Then pass `lease.Generation` and `lease.RefreshToken` to the `RunAsync` call.

- [x] 3.6 Same file, `PopulateBlockedStateAsync` method (lines 499-544): replace the guard `!cancellationToken.IsCancellationRequested && generation == this.blockedStateGeneration` (line 524) with `this.blockedStatePass.IsCurrent(generation, cancellationToken)`.

- [x] 3.7 Same file, `Dispose` method (lines 352-364): replace the two generation-increment + cancel/dispose/null sequences (lines 354-357 for version, 358-361 for blocked-state) with:
  ```csharp
  this.versionPopulationPass.Dispose();
  this.blockedStatePass.Dispose();
  ```

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — all tests green, including existing `AgentManagementViewModelTests` (version and blocked-state population tests).

## Stage 4: Consolidate busy-state raisers in `AgentManagementViewModel`

**Goal:** Extract `RaiseHeaderGateChanges`, redefine `RaiseCardActionCountChanges` and `RaiseBusyStateChanges` to use it, and unify the gate properties through `IsAnyPassRunning`. No prologue/finally changes yet.

- [x] 4.1 `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`: add a private property (after the existing `IsCardActionRunning` property):
  ```csharp
  /// <summary>
  ///   Whether any pass or per-card action is currently running: true while Detect, Check updates, or a per-card action runs.
  /// </summary>
  private bool IsAnyPassRunning => this.IsDetecting || this.IsCheckingUpdates || this.IsCardActionRunning;
  ```

- [x] 4.2 Same file: add a new private method `RaiseHeaderGateChanges` (place near the existing `RaiseCardActionCountChanges` and `RaiseBusyStateChanges`):
  ```csharp
  /// <summary>
  ///   Raises property-changed and command-changed notifications for the header-wide Detect and Check updates gates.
  /// </summary>
  private void RaiseHeaderGateChanges()
  {
      this.OnPropertyChanged(nameof(this.CanDetect));
      this.OnPropertyChanged(nameof(this.CanCheckUpdates));
      this.DetectCommand.RaiseCanExecuteChanged();
      this.CheckUpdatesCommand.RaiseCanExecuteChanged();
  }
  ```

- [x] 4.3 Same file, redefine `RaiseCardActionCountChanges` (lines 256-263) to call `RaiseHeaderGateChanges()`:
  ```csharp
  private void RaiseCardActionCountChanges()
  {
      this.OnPropertyChanged(nameof(this.IsCardActionRunning));
      this.RaiseHeaderGateChanges();
  }
  ```

- [x] 4.4 Same file, redefine `RaiseBusyStateChanges` (lines 395-403) to call `RaiseHeaderGateChanges()` plus the per-card commands:
  ```csharp
  private void RaiseBusyStateChanges()
  {
      this.RaiseHeaderGateChanges();
      this.DetectCardCommand.RaiseCanExecuteChanged();
      this.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
  }
  ```

- [x] 4.5 Same file, redefine `CanDetect` (line 202) to use `IsAnyPassRunning`:
  ```csharp
  public bool CanDetect => !this.IsAnyPassRunning;
  ```

- [x] 4.6 Same file, redefine `CanCheckUpdates` (line 220) to use `IsAnyPassRunning`:
  ```csharp
  public bool CanCheckUpdates => !this.IsAnyPassRunning;
  ```

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — all tests green, including the CanExecuteChanged ordering tests (`DetectCommand_RaisesCanExecuteChangedAtPassStartAndEnd`, `CheckUpdatesCommand_RaisesCanExecuteChangedAtPassStartAndEnd`, `CheckCardUpdatesCommand_RaisesCanExecuteChangedAtCheckStartAndEnd`, `DetectCardCommand_RaisesCanExecuteChangedAtDetectStartAndEnd`).

## Stage 5: Add `CardActionScope` and migrate per-card actions

**Goal:** Extract the per-card prologue/finally into `CardActionScope` and use it in `DetectCardAsync` and `CheckCardUpdatesAsync`. No try/catch or exception-restore changes.

- [x] 5.1 `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`: add a private nested class `CardActionScope` (place near the end of the class, before the closing brace). Implement per the design API, with the WPF caching comment moved into `Dispose()`.

- [x] 5.2 Same file, `DetectCardAsync` method (lines 546-585): replace the prologue (lines 548-551) and `finally` block (lines 574-584) with a `using var scope = new CardActionScope(this, busy => card.IsDetecting = busy);` at the method start (before the `var previousState` line). The prologue/finally logic moves into the scope's constructor and `Dispose()`. Keep the try/catch and exception-restore logic (lines 568-573) unchanged. The scope owns the counter/command raises, so no lambdas for `EnterCardAction`/`ExitCardAction` are passed.

- [x] 5.3 Same file, `CheckCardUpdatesAsync` method (lines 690-736): replace the prologue (lines 702-705) and `finally` block (lines 725-735) with `using var scope = new CardActionScope(this, busy => card.IsCheckingUpdates = busy);` (after the early-return guard for `NotInstalled`, before the `try` block). Keep the try block (lines 706-724) unchanged. The scope owns the counter/command raises, so no lambdas for `EnterCardAction`/`ExitCardAction` are passed.

- [x] 5.4 No new scope test is required: the existing `DetectCardCommand_RaisesCanExecuteChangedAtDetectStartAndEnd` and `CheckCardUpdatesCommand_RaisesCanExecuteChangedAtCheckStartAndEnd` tests pin the enter/exit notification order end-to-end. Do not add a production seam just to unit-test the private scope.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — all tests green, including the CanExecuteChanged ordering tests and any new `CardActionScope` test.

## Stage 6: Final verification and refresh graphify

**Goal:** Confirm the entire test suite passes, run any manual smoke checks, and update the project knowledge graph.

- [x] 6.1 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

- [x] 6.2 Run `dotnet test CLIHub.sln` — confirm all 1,263+ tests green. If any test fails, diagnose before proceeding.

- [x] 6.3 Manual smoke (user-confirmed 2026-10-30): launch the app, open Settings → CLI Agents, trigger header Detect and Check updates and per-card Detect/Check updates, close Settings mid-pass, and confirm no stuck busy state or log errors.

- [x] 6.4 Run `graphify update .` to refresh the project knowledge graph with the new primitives and refactored code.

**Gate:** All tests green, no build warnings, graphify updated.

**Done:** The refactor is complete. Report to the user: all stages passed, primitives added, duplication removed, behavior preserved. Ask whether to commit and archive.
