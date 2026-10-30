# Design

## Verified Current State

All citations verified against the working tree on 2026-10-30. The working tree has unrelated uncommitted docs changes (`docs/backlog.md` modified; four `docs/architecture-analysis-*.md` deleted); those are not touched by this change.

### `AgentManagementViewModel.cs` (823 lines)

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, the Settings → CLI Agents page (DI transient, `src/CLIHub/Composition/ServiceRegistration.cs:165`).

**Pass 1: Version population**

- Fields: `versionPopulationCts` and `versionPopulationGeneration` (lines not explicitly visible in snippets, but referenced in methods).
- `PopulateVersions` (`:413-426`): cancels/disposes `versionPopulationCts`, creates a new one, bumps `++versionPopulationGeneration`, captures the token, snapshots `Cards.ToList()`, and starts `RunAsync("Agent version population", ...)`.
- `PopulateVersionsAsync` (`:428-482`): creates a linked CTS from refresh + application tokens (`:434-437`), iterates in parallel via `Task.WhenAll`, guards writes with `!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration` (`:447`, `:456`, `:464`, `:470`), catches expected cancellation silently (`:475-477`), logs unexpected errors (`:478-481`) with message `"Agent version population failed"`.

**Pass 2: Blocked-state population**

- Fields: `blockedStateCts` and `blockedStateGeneration` (referenced in methods).
- `PopulateBlockedState` (`:484-497`): same shape as `PopulateVersions` — cancel/dispose/create/bump/capture/snapshot/start `RunAsync("Agent blocked-state population", ...)`.
- `PopulateBlockedStateAsync` (`:499-544`): creates linked CTS (`:505-508`), guards write with `!cancellationToken.IsCancellationRequested && generation == this.blockedStateGeneration` (`:524`), catches expected cancellation silently (`:537-539`), logs unexpected errors (`:540-543`) with message `"Agent blocked-state population failed"`.

**Dispose** (`:352-364`): increments `versionPopulationGeneration` (`:354`), cancels/disposes/nulls `versionPopulationCts` (`:355-357`); then increments `blockedStateGeneration` (`:358`), cancels/disposes/nulls `blockedStateCts` (`:359-361`). Also unsubscribes from `PluginsChanged` (`:362`) and sets `disposed = true` (`:363`).

**Per-card busy state**

- `CanDetect` (`:202`): `!this.IsDetecting && !this.IsCheckingUpdates && !this.IsCardActionRunning`
- `CanCheckUpdates` (`:220`): `!this.IsDetecting && !this.IsCheckingUpdates && !this.IsCardActionRunning` — identical to `CanDetect`.
- `EnterCardAction` (`:240-244`): increments `runningCardActionCount`, calls `RaiseCardActionCountChanges()`.
- `ExitCardAction` (`:250-254`): decrements `runningCardActionCount`, calls `RaiseCardActionCountChanges()`.
- `RaiseCardActionCountChanges` (`:256-263`): raises `IsCardActionRunning`, `CanDetect`, `CanCheckUpdates`, `DetectCommand.CanExecuteChanged`, `CheckUpdatesCommand.CanExecuteChanged`.
- `RaiseBusyStateChanges` (`:395-403`): raises `CanDetect`, `CanCheckUpdates`, `DetectCommand.CanExecuteChanged`, `CheckUpdatesCommand.CanExecuteChanged`, `DetectCardCommand.CanExecuteChanged`, `CheckCardUpdatesCommand.CanExecuteChanged`.

**Per-card action prologue and finally**

- `DetectCardAsync` (`:546-585`):
  - Prologue (`:548-551`): sets `card.IsDetecting = true`, calls `EnterCardAction()`, raises both card commands' `CanExecuteChanged`.
  - Finally (`:574-584`): clears `card.IsDetecting = false` FIRST (with the WPF caching comment), calls `ExitCardAction()`, raises both card commands' `CanExecuteChanged`.

- `CheckCardUpdatesAsync` (`:690-736`):
  - Prologue (`:702-705`): sets `card.IsCheckingUpdates = true`, calls `EnterCardAction()`, raises both card commands' `CanExecuteChanged` — character-for-character identical to `DetectCardAsync` prologue.
  - Finally (`:725-735`): clears `card.IsCheckingUpdates = false` FIRST (with the same WPF caching comment), calls `ExitCardAction()`, raises both card commands' `CanExecuteChanged` — character-for-character identical to `DetectCardAsync` finally.

### `AgentPaneController.cs`

`src/CLIHub/ViewModels/Agents/AgentPaneController.cs`, the launch-window agent pane (DI singleton, `src/CLIHub/Composition/ServiceRegistration.cs:108`).

- Fields: `versionPopulationCts` and `versionPopulationGeneration` (`:23-24`).
- `Refresh` (`:92-96`): cancels/disposes the old CTS, creates a new one, bumps `++versionPopulationGeneration`, captures the token, then starts `operationLifetime.RunAsync("Agent version population", ...)`.
- `PopulateVersionsAsync` (`:164-193`): creates linked CTS from refresh + application tokens (`:170-173`), probes in parallel, guards writes with `!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration` (`:180`), catches expected cancellation silently (`:186-188`), logs unexpected errors (`:189-192`) with message `"Agent version population failed"`.
- `Dispose` (`:149-162`): increments `versionPopulationGeneration` (`:157`), cancels/disposes/nulls `versionPopulationCts` (`:158-160`), unsubscribes from `PluginsChanged` (`:161`).

### Test coverage

`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` (2,347 lines):

- `DetectCommand_RaisesCanExecuteChangedAtPassStartAndEnd` (`:1337-1386`): asserts that `DetectCommand`, `CheckUpdatesCommand`, and `DetectCardCommand` all raise `CanExecuteChanged` exactly once at pass start and once at pass end, and validates the busy/idle states.
- `CheckUpdatesCommand_RaisesCanExecuteChangedAtPassStartAndEnd` (`:1389-1441`): same structure for `CheckUpdatesCommand`.
- `CheckCardUpdatesCommand_RaisesCanExecuteChangedAtCheckStartAndEnd` (`:2150-2188`): validates per-card check updates' notification order.
- `DetectCardCommand_RaisesCanExecuteChangedAtDetectStartAndEnd` (`:2218-2256`): validates per-card detect's notification order.

These tests pin the exact notification order and per-card independence, which the refactor must preserve.

`tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs` pins refresh/selection/version behavior.

### Existing shared primitives

`src/CLIHub/ViewModels/Shared/` contains:
- `PromptState.cs` — tracks modal prompt depth with a nested `Scope` returned from `Begin()`.
- `StatusMessageCoordinator.cs` — coordinates status messages.
- `WindowActionCoordinator.cs` — coordinates window actions.

All are in namespace `CLIHub.ViewModels`.

## Decisions

### D1: Extract `RestartablePass` as a shared primitive

Add `RestartablePass` under `src/CLIHub/ViewModels/Shared/` with namespace `CLIHub.ViewModels`. It owns:
- One `CancellationTokenSource?` field and one `int` generation counter.
- `PassLease Begin()`: cancels/disposes the old CTS (if any), creates a new `CancellationTokenSource`, increments the generation, returns a record struct `PassLease(int Generation, CancellationToken RefreshToken)`.
- `bool IsCurrent(int generation, CancellationToken token)`: returns `!token.IsCancellationRequested && generation == <current generation>`.
- `void Dispose()`: increments the generation, cancels/disposes/nulls the CTS (idempotent).

**Threading model:** UI-thread calls only (no locking), matching the existing fields' assumptions.

**Rationale:** the cancel-create-bump-capture sequence is identical across all four current usages. The primitive must not own the linked CTS (that lives in each async method, linking refresh + application tokens), must not own the operation name (that varies: "Agent version population", "Agent blocked-state population"), and must not absorb logging (each caller logs its own message and operation context). The primitive is just the invalidation and guard logic.

### D2: Each caller keeps its linked CTS, operation name, and logging

`PopulateVersionsAsync` and `PopulateBlockedStateAsync` in both view models create their own linked `CancellationTokenSource` (combining the refresh token from `Begin()` with the application token), keep their own `RunAsync` operation name, and keep their own local `catch (OperationCanceledException)` and `catch (Exception)` with surface-specific log messages.

**Rationale:** the operation name and log message are domain-specific; the linked CTS is standard async composition; local OCE catches are required to keep surface-local cancellation (refresh supersede, page dispose) out of the operation-lifetime runner's generic Error catch (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:98-105` — see `2026-10-24-consolidate-version-population/design.md:70-80` for the rationale). The primitive does not replace those — it only consolidates the generation/invalidation pattern.

### D3: Add `CardActionScope` as a private nested type in `AgentManagementViewModel`

`CardActionScope` encapsulates the prologue/finally:
- Constructor: sets the card's busy flag (passed as a settable property or delegate), calls `EnterCardAction()`, raises both card command `CanExecuteChanged`.
- `Dispose`: clears the card's busy flag FIRST (move the WPF caching comment into the type), calls `ExitCardAction()`, raises both card command `CanExecuteChanged`.

**Scope:** private nested type in `AgentManagementViewModel`; not a Shared type, since it is tightly coupled to that view model's per-card state and commands.

**Rationale:** the prologue/finally blocks in `DetectCardAsync` and `CheckCardUpdatesAsync` are character-for-character identical (`:548-551` vs `:702-705`, `:574-584` vs `:725-735`), including the same comment. The scope preserves the exact order (busy flag BEFORE counter, clear busy flag BEFORE notifications) and moves the rationale comment to one place. The outer try/catch in each method keeps its current shape — the scope only replaces the prologue + finally, not the exception/restore logic (which is parked in the backlog as a separate concern).

### D4: Consolidate the busy-state raisers without changing notification sets

Current state:
- `RaiseCardActionCountChanges` (`:256-263`): `IsCardActionRunning` + `CanDetect` + `CanCheckUpdates` + `DetectCommand` + `CheckUpdatesCommand`.
- `RaiseBusyStateChanges` (`:395-403`): `CanDetect` + `CanCheckUpdates` + `DetectCommand` + `CheckUpdatesCommand` + `DetectCardCommand` + `CheckCardUpdatesCommand`.
- `CanDetect` and `CanCheckUpdates` are identical (`:202`, `:220`).

Refactor:
- Extract `RaiseHeaderGateChanges()`: `CanDetect` + `CanCheckUpdates` + `DetectCommand` + `CheckUpdatesCommand`.
- Redefine `RaiseCardActionCountChanges()`: `IsCardActionRunning` + `RaiseHeaderGateChanges()`.
- Redefine `RaiseBusyStateChanges()`: `RaiseHeaderGateChanges()` + `DetectCardCommand` + `CheckCardUpdatesCommand`.
- Unify the gate properties: introduce `IsAnyPassRunning => IsDetecting || IsCheckingUpdates || IsCardActionRunning`; `CanDetect` and `CanCheckUpdates` both become `!IsAnyPassRunning`.

**Notification order table (preserved):**

| Trigger | Current raises | After refactor | Same? |
|---------|---------------|----------------|-------|
| Pass start/end (`IsDetecting`/`IsCheckingUpdates` flip) | `CanDetect`, `CanCheckUpdates`, `DetectCommand`, `CheckUpdatesCommand`, `DetectCardCommand`, `CheckCardUpdatesCommand` | Same (via `RaiseBusyStateChanges` → `RaiseHeaderGateChanges` + two card commands) | ✓ |
| Card action enter/exit (`IsCardActionRunning` change) | `IsCardActionRunning`, `CanDetect`, `CanCheckUpdates`, `DetectCommand`, `CheckUpdatesCommand` | Same (via `RaiseCardActionCountChanges` → `IsCardActionRunning` + `RaiseHeaderGateChanges`) | ✓ |

**Rationale:** the raisers overlap; extracting the common prefix removes duplication without changing the notification set or order. The unified `IsAnyPassRunning` makes the two identical gate properties explicit and names the intent. The existing CanExecuteChanged ordering tests (`AgentManagementViewModelTests.cs:1337-1386`, `:1389-1441`, `:2150-2188`, `:2218-2256`) will catch any ordering regression.

### D5: Scope exclusions (per the brief)

- Do NOT change the `disposed` guards in `DetectAsync`/`CheckUpdatesAsync`/`DetectCardAsync`/`CheckCardUpdatesAsync` (parked backlog).
- Do NOT change `AgentCardItem` setters, public interfaces, XAML, or logging text.
- Do NOT touch `AgentManagementService` or Core.
- Do NOT add production seams for tests; the primitives are simple enough for direct unit tests, and the existing integration tests provide the safety net.

## Primitive APIs

### `RestartablePass`

Location: `src/CLIHub/ViewModels/Shared/RestartablePass.cs`

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Manages a restartable pass with cancellation-token lifecycle and generation-based staleness detection.
///   Supports the pattern where a new pass supersedes an in-flight pass and only the current generation's results are applied.
/// </summary>
internal sealed class RestartablePass : IDisposable
{
    private CancellationTokenSource? cts;
    private int generation;

    /// <summary>
    ///   Begins a new pass: invalidates any in-flight pass, creates a new cancellation source, increments the generation,
    ///   and returns a lease containing the generation and refresh token.
    /// </summary>
    /// <returns> A lease containing the current generation and refresh token. </returns>
    public PassLease Begin()
    {
        this.cts?.Cancel();
        this.cts?.Dispose();
        this.cts = new CancellationTokenSource();
        var gen = ++this.generation;
        return new PassLease(gen, this.cts.Token);
    }

    /// <summary>
    ///   Checks whether a pass lease is still current: the token is not cancelled and the generation matches.
    /// </summary>
    /// <param name="generation"> The generation from the lease. </param>
    /// <param name="token"> The cancellation token from the lease (or a linked token). </param>
    /// <returns> True if the pass is current; false if superseded or cancelled. </returns>
    public bool IsCurrent(int generation, CancellationToken token)
        => !token.IsCancellationRequested && generation == this.generation;

    /// <summary>
    ///   Disposes the pass: invalidates the generation, cancels, disposes, and nulls the current source.
    ///   Idempotent; safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        this.generation++;
        this.cts?.Cancel();
        this.cts?.Dispose();
        this.cts = null;
    }
}

/// <summary>
///   A lease returned from <see cref="RestartablePass.Begin" />, containing the generation and refresh token for one pass.
/// </summary>
/// <param name="Generation"> The generation counter at the time the pass started. </param>
/// <param name="RefreshToken"> The refresh-scoped cancellation token; cancelled when a new pass begins or the pass is disposed. </param>
internal readonly record struct PassLease(int Generation, CancellationToken RefreshToken);
```

### `CardActionScope` (nested in `AgentManagementViewModel`)

```csharp
/// <summary>
///   Scope for a per-card action: sets the card's busy flag, increments the running-action counter, and raises
///   the gated commands on enter; clears the busy flag BEFORE decrementing and raising on exit.
///   The busy flag is cleared first because WPF caches the CanExecute result during CanExecuteChanged; clearing
///   before raising ensures the re-query sees the card idle.
/// </summary>
private sealed class CardActionScope : IDisposable
{
    private readonly AgentManagementViewModel owner;
    private readonly Action<bool> setBusy;
    private bool disposed;

    public CardActionScope(AgentManagementViewModel owner, Action<bool> setBusy)
    {
        this.owner = owner;
        this.setBusy = setBusy;

        // Prologue: set busy, enter, raise the card commands.
        setBusy(true);
        owner.EnterCardAction();
        owner.DetectCardCommand.RaiseCanExecuteChanged();
        owner.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        // Clear the card's busy flag BEFORE the completion raises: WPF caches the
        // CanExecute result it queries during CanExecuteChanged, so a raise that still
        // sees the card busy would cache false and leave the button disabled until an
        // unrelated input event re-queries it.
        this.setBusy(false);
        this.owner.ExitCardAction();
        this.owner.DetectCardCommand.RaiseCanExecuteChanged();
        this.owner.CheckCardUpdatesCommand.RaiseCanExecuteChanged();
    }
}
```

**Usage in `DetectCardAsync`:**

```csharp
using var scope = new CardActionScope(this, busy => card.IsDetecting = busy);
```

(Analogous for `CheckCardUpdatesAsync` with `card.IsCheckingUpdates`.)

## Alternatives Considered and Rejected

### Alt1: Shared version-population engine

Extract a shared helper that runs the parallel probe loop and guards writes.

**Rejected:** the two surfaces differ in item type (`AgentItem` vs `AgentCardItem`, both WPF view-model types), per-card steps (availability check, option rebuild), and write targets. A shared helper would need generics or delegates over those concerns, which is more complex than the current local loops. The primitive (`RestartablePass`) only consolidates the invalidation/guard pattern, not the entire loop.

### Alt2: Make `CardActionScope` a shared type

Place `CardActionScope` in `ViewModels/Shared/` as a reusable primitive.

**Rejected:** the scope is tightly coupled to `AgentManagementViewModel`'s per-card state, commands, and counter. No other view model has this exact pattern (the pane controller has no per-card actions). A shared type would need to abstract those concerns, which would make it more complex without a second consumer. Keeping it private to the one consumer is simpler.

### Alt3: Merge `CanDetect` and `CanCheckUpdates` into one property

Replace the two identical properties with a single `CanPerformHeaderAction`.

**Rejected:** the properties are exposed on the view model's public interface and are bound by XAML. Renaming them would be a public API change and is out of scope for a behavior-preserving refactor. The refactor only unifies their implementation through `IsAnyPassRunning`, not their names.

## Risks

### R1: Notification order regression

**Mitigation:** the existing CanExecuteChanged ordering tests (`AgentManagementViewModelTests.cs:1337-1386`, `:1389-1441`, `:2150-2188`, `:2218-2256`) pin the exact start/end order and per-card independence. The refactor preserves the notification sets and calls in the notification-order table (Decision D4); if the order regresses, those tests will fail.

### R2: WPF caching regression

**Mitigation:** the WPF caching rationale (clear the busy flag BEFORE raising `CanExecuteChanged`) is preserved in `CardActionScope.Dispose()` with the existing comment moved into the type. The order (clear flag → decrement counter → raise) is unchanged.

### R3: Threading assumptions

**Mitigation:** `RestartablePass` and `CardActionScope` inherit the existing threading model: UI-thread calls only, no new locks. The refactor does not introduce concurrency; it only moves the existing single-threaded logic into reusable types.

## Open Questions

None. The refactor is mechanical: the primitives reproduce the current logic, and the existing test suite (1,263 tests including CanExecuteChanged ordering) is the safety net.

## Deviations from Brief

None. All line numbers and code structure match the working tree as cited.
