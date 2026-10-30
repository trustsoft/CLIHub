# Consolidate pass management plumbing in AgentManagementViewModel and AgentPaneController

## Why

`AgentManagementViewModel` and `AgentPaneController` each contain duplicated restartable-pass plumbing and per-card busy-state plumbing that can be consolidated without changing any observable behavior, public API, or operation names:

- **Restartable-pass plumbing** is duplicated between `PopulateVersions` and `PopulateBlockedState` in `AgentManagementViewModel` (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:413-482`, `:484-544`), and mirrored in `AgentPaneController` (`src/CLIHub/ViewModels/Agents/AgentPaneController.cs:92-96`, `:164-193`). Each owns a `CancellationTokenSource` + generation counter, creates linked cancellation tokens, repeats the same `!token.IsCancellationRequested && generation == this.<field>Generation` guard, and has identical `Dispose` sequences (cancel, dispose, null, increment generation).

- **Per-card busy-state plumbing** is duplicated in `DetectCardAsync` and `CheckCardUpdatesAsync` (`AgentManagementViewModel.cs:548-551`, `:702-705` for prologues; `:574-584`, `:725-735` for `finally` blocks — character-for-character identical, including the WPF caching comment). `CanDetect` and `CanCheckUpdates` are identical (`:202`, `:220`); `EnterCardAction`/`ExitCardAction`/`RaiseCardActionCountChanges` (`:240-263`) and `RaiseBusyStateChanges` (`:395-403`) have overlapping notification sets.

This is mechanical duplication, not domain variation: every pass follows the same invalidation pattern, and every card action follows the same busy-flag + notification order. Extracting the primitives removes the duplication without changing behavior.

## What Changes

**Behavior:**

None. This is a behavior-preserving internal refactor. All existing guards, notification orders, operation names, and log messages are preserved. No public API, XAML, or test change beyond adding focused unit tests for the new primitives.

**Code:**

- Add `RestartablePass` under `src/CLIHub/ViewModels/Shared/` (namespace `CLIHub.ViewModels`). It owns one `CancellationTokenSource` + generation counter and exposes:
  - `PassLease Begin()`: cancels/disposes the previous source, creates a new one, increments the generation, returns the generation + refresh token.
  - `bool IsCurrent(int generation, CancellationToken token)`: the existing `!token.IsCancellationRequested && generation == <current>` check.
  - `Dispose()`: invalidates the generation, cancels, disposes, nulls the source (idempotent).

- `AgentManagementViewModel.PopulateVersions` and `PopulateBlockedState` use `RestartablePass` instead of their own fields; the `Dispose` method uses `RestartablePass.Dispose()`. The linked CTS, operation name, try/catch, and exact log message stay in each caller.

- `AgentPaneController` follows the same migration.

- Add a `CardActionScope` (private nested type in `AgentManagementViewModel`) that encapsulates the per-card prologue/finally: set the card's busy flag, increment the running-card-action counter, raise the gate changes, raise both card command changes; on `Dispose`, clear the card flag FIRST, decrement, raise the gate changes, raise both card command changes. `DetectCardAsync` and `CheckCardUpdatesAsync` use `using var scope = ...` and keep their try/catch state-restore semantics.

- Deduplicate the busy raisers:
  - `RaiseHeaderGateChanges()` = `OnPropertyChanged(CanDetect)`, `OnPropertyChanged(CanCheckUpdates)`, `DetectCommand.RaiseCanExecuteChanged()`, `CheckUpdatesCommand.RaiseCanExecuteChanged()`.
  - `RaiseCardActionCountChanges()` = `OnPropertyChanged(IsCardActionRunning)` + `RaiseHeaderGateChanges()`.
  - `RaiseBusyStateChanges()` = `RaiseHeaderGateChanges()` + `DetectCardCommand.RaiseCanExecuteChanged()` + `CheckCardUpdatesCommand.RaiseCanExecuteChanged()`.
  - `CanDetect` and `CanCheckUpdates` both become `!this.IsAnyPassRunning`, where `IsAnyPassRunning => IsDetecting || IsCheckingUpdates || IsCardActionRunning`.

## Impact

**User-visible:**

None. This is an internal refactor; no observable behavior, UI text, layout, or public API changes.

**Internal:**

- `RestartablePass` is added as a new internal type under `ViewModels/Shared/`.
- `CardActionScope` is added as a private nested type in `AgentManagementViewModel`.
- `AgentManagementViewModel` and `AgentPaneController` each lose two CTS/generation field pairs and gain one or two `RestartablePass` instances; busy-state raisers are consolidated.
- The exact notification order, exception handling, logging text, and operation names are preserved.
- No Core, service, or interface changes. No XAML changes.

**Tests:**

- The existing 1,263-test suite, including the CanExecuteChanged ordering tests (`AgentManagementViewModelTests.cs:1337-1386`, `:1389-1441`, `:2150-2188`, `:2218-2256`), is the safety net — every test stays green.
- Add focused unit tests for `RestartablePass` (second `Begin` invalidates the first lease; `Dispose` invalidates; `IsCurrent` respects cancellation) and `CardActionScope` if the existing CanExecuteChanged tests do not already prove the enter/exit order.

## Non-goals

- Changing `AgentCardItem` setters, public interfaces, XAML, or logging text.
- Changing the `disposed` guards or the exception/restore paths in `DetectAsync`/`CheckUpdatesAsync`/`DetectCardAsync`/`CheckCardUpdatesAsync` — parked backlog items cover those.
- Touching `AgentManagementService`, Core services, or any other ViewModel.
- Adding production seams for tests; the primitives are designed for direct unit tests and existing integration tests provide the regression safety net.
- Changing test assertions or mock setup beyond adding focused new tests for the primitives.

## Dependencies

None. This is self-contained within `AgentManagementViewModel`, `AgentPaneController`, and the new shared primitives. No other active changes affect these files or their busy-state contracts.
