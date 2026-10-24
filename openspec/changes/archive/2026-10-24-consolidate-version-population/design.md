# Design

## Verified Current State

All citations verified against the working tree at `a925d6b` (only `docs/backlog.md` carries
unrelated pre-existing edits). The backlog entry (`docs/backlog.md:62-70`) is **not drifted**:
the three loops exist exactly as parked. One claim in the dispatching brief is slightly off —
see "Discrepancies".

### Loop 1 (live): `AgentPaneController` — the canonical shape

`src/CLIHub/ViewModels/Agents/AgentPaneController.cs`, the launch-window agent pane (DI
singleton, `src/CLIHub/Composition/ServiceRegistration.cs:108`; used by
`src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:24`).

- Fields: `versionPopulationCts` + `versionPopulationGeneration` (`:21-22`).
- `Refresh` (`:78-117`): cancels and disposes the old CTS, creates a new one, bumps the
  generation, captures the token (`:87-91`), synchronizes rows, then starts the pass through
  `operationLifetime.RunAsync("Agent version population", applicationToken => …)` with the
  row snapshot, generation, refresh token, and application token (`:105-114`).
- `PopulateVersionsAsync` (`:159-188`): links refresh + application tokens
  (`:165-168`), probes all agents in parallel via `Task.WhenAll` (`:172-179`), writes
  `version ?? "unknown"` only when `!cancellationToken.IsCancellationRequested && generation ==
  this.versionPopulationGeneration` (`:175-177`).
- `Dispose` (`:144-157`): bumps the generation, cancels and disposes the CTS.
- Failure handling (`:181-187`): expected cancellation is swallowed silently; **everything else
  is swallowed into `Debug.WriteLine`** (`:186`) — it never reaches the operation-lifetime
  runner's logger. The class has no logger; `using System.Diagnostics` (`:4`) exists only for
  this line.

### Loop 2 (live): `AgentManagementViewModel` — the divergent shape

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, the Settings → CLI Agents page
(DI transient, `ServiceRegistration.cs:165`; created with the transient `SettingsViewModel`
`:169-182`).

- `PopulateVersions` (`:259-283`), called from `Load` (`:224`): runs inside
  `RunAsync("Agent version population", …)` (`:260`, `:362-363` — the application token only),
  iterates **serially** over a `this.Cards.ToList()` snapshot (`:262`), and per card awaits
  `GetVersionAsync` then `CheckAsync` (`:264`, `:272`), writing
  `card.Version = ver ?? "unknown"` and `card.UpdateAvailability` guarded only by
  `!this.disposed` (`:265-267`, `:273-276`); finally rebuilds the default-agent options, also
  `disposed`-guarded (`:279-282`).
- No CTS of its own, no generation. `Dispose` (`:228-231`) only sets `disposed = true` — the
  pass keeps issuing per-card availability checks (each an HTTP release request,
  `src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:48`) after the page is gone.
- Failure routing today: there is **no local catch** — an exception propagates to
  `ApplicationOperationLifetime.RunObservedAsync`, which logs
  `LogError("Application operation failed: {OperationName}")`
  (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:102-105`). So this loop already routes
  failures to `ILogger` (via the runner); it does not swallow into `Debug.WriteLine`.
- The class already has `ILogger<AgentManagementViewModel>` injected (`:30`, `:55`, `:65`) and
  uses it (`LogWarning` in `EditConfig`, `:357`).
- Lifetime wiring: `SettingsWindow.Closed → viewModel.Dispose()`
  (`src/CLIHub/Views/SettingsWindow.xaml.cs:30`) → `AgentsState.Dispose()`
  (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:1785`).
- **Re-entrant `Load` is a real production path**: `SettingsLauncher` reuses one window
  (`src/CLIHub/Settings/SettingsLauncher.cs:26-35`) and every activation calls
  `ShowSettings → viewModel.Load()` (`SettingsWindow.xaml.cs:38-40`) →
  `AgentsState.Load` (`SettingsViewModel.cs:1388`). A slow first pass can therefore still be
  running when `Load` runs again on the same live view model.

### Loop 3 (dead): legacy `MainWindow`

`src/CLIHub/Views/MainWindow.xaml.cs:194-217` — class remarks: "Deprecated legacy reference
window. It is not registered or constructed by the application." (`:22-24`). It already has the
generation+CTS shape (`:37-38`, `:150-154`) and the same `Debug.WriteLine` swallow (`:213-216`).
Confirmed untouched by this change per the parked remedy.

### The fire-and-forget boundary both live loops share

- `ApplicationOperationLifetime.RunAsync` tracks the operation
  (`ApplicationOperationLifetime.cs:36-53`); `RunObservedAsync` catches
  `OperationCanceledException` **only when the lifetime's own token is cancelled**
  (`:98-101`, logged as Information) and everything else as Error (`:102-105`).
- Consequence: a *surface-local* cancellation (refresh supersede, page close) thrown out of a
  loop would land in the generic catch and be logged as an **Error**. Each loop therefore needs
  its own local `catch (OperationCanceledException) when (<surface token>.IsCancellationRequested)`
  filter — this is why the pane's local OCE catch must survive any refactor, and why the
  management loop needs one once it owns a CTS.

### Probe semantics (what cancellation can and cannot stop)

- `AgentVersionService.GetVersionAsync` (`src/CLIHub.Core/Agents/AgentVersionService.cs:52-77`)
  coalesces concurrent probes through an in-flight `Lazy` map and observes cancellation only via
  `WaitAsync(cancellationToken)` (`:68`). The probe process itself runs with
  `CancellationToken.None` and the configured probe timeout (`:89-90`) — deliberately, so
  coalescing callers share one result. Per-probe command failures are logged and cached as
  `null` (`:92-100`), so exceptions reaching the loop catches are unexpected infrastructure
  errors, not probe outcomes.
- Canceling the management pass therefore stops the loop from *starting further* lookups and
  availability checks; an in-flight probe finishes and caches (reusable by the next pass), which
  is the pane's existing semantic.

### Sentinel/display facts (preserved verbatim)

- `AgentItem.Version` defaults to `"…"` (`src/CLIHub/ViewModels/Agents/AgentItem.cs:107`);
  management cards are created with `"…"` (`AgentManagementViewModel.cs:212`), and the chip
  treats `…`/`unknown` specially (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:19`, `:194`;
  `AgentManagementViewModel.cs:119`). This is the separate "Magic version sentinels" backlog
  item (`docs/backlog.md:72-78`) — out of scope.

### Specs

- `openspec/specs/agent-version/spec.md:97-107` — "Stale version population does not update the
  current list" with scenarios "Refresh supersedes a population pass" and "Current population
  completes". The management page satisfies both today **by snapshot isolation** (each `Load`
  builds new card objects; an older pass's snapshot is detached, so its writes are invisible),
  not by an explicit guard — no scenario is violated, but the guarantee is accidental.
- "Non-blocking presentation" (`:65-75`) and "Unknown version display" (`:77-83`) hold for both
  loops (serial fill-in satisfies "Versions fill in" per agent; parallel does too).
- `openspec/specs/logging/spec.md:69-79` — "Service logging integration": services receive
  `ILogger<T>` through DI and log through it. Injecting `ILogger<AgentPaneController>` needs no
  registration change (`AddLogging` + Serilog at `ServiceRegistration.cs:55`).
- `openspec/specs/agent-ui/spec.md:248-297` — "Agent card displayed content": the card chip
  contract is untouched by this change.

### Test topology

- `AgentPaneControllerTests` constructs the controller directly at five sites
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs:27`, `:54`, `:84`, `:118`,
  `:144`); `Dispose_UnsubscribesAndCancelsVersionPopulation` (`:107-132`) mocks `RunAsync` as
  no-execute (`:115-117`), so the loop body never runs there.
- `StatusMessageCoordinatorTests` (`:118`, `:160`, `:223`) and
  `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` (`:255`, `:328`) also construct
  it; `tests/CLIHub.Tests/README.md:55,209` shows the constructor in examples.
- `AgentManagementViewModelTests.CreateViewModel`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:36-70`) uses
  `NullLogger<AgentManagementViewModel>` (`:69`) and an `InlineOperationLifetime` (`:68`,
  defined `:464-477`) that runs the operation synchronously to completion — existing population
  tests (`Load_PopulatesVersions` `:164-171`, `Load_PopulatesUpdateAvailabilityPerCard`
  `:174-188`) rely on that inline completion and stay valid for a `Task.WhenAll` body with
  mocked (already-completed) service tasks.

## Decisions

### D1: One canonical shape — the pane's — for both live loops

The management `PopulateVersions` is rewritten to the pane's structure: cancel-and-replace the
surface CTS, bump the generation, snapshot the items, run inside
`operationLifetime.RunAsync` with a linked refresh+application token, probe in parallel, and
write only under `!cancelled && generation == current`. The management loop keeps its
surface-specific steps *inside* that shape: the per-card availability check (version applied as
soon as it resolves, availability applied after its check — preserving the current per-card
ordering) and the final `RebuildDefaultAgentOptions` (generation-guarded, run only on the
unwound-success path, matching today's behavior where an exception skips the rebuild).

**Rationale:** the pane's shape is the one the backlog names, it is the shape the
`agent-version` stale-population requirement describes, and it makes the management guarantee
hold by construction (explicit guard) instead of by snapshot accident — which matters because
re-entrant `Load` on a live view model is a real path (Settings re-activation), not a theory.

### D2: Failure routing — injected `ILogger<T>` in both loops, local catches kept

- `AgentPaneController` gains `ILogger<AgentPaneController>` (constructor +1); the
  `Debug.WriteLine` catch becomes `this.logger.LogError(ex, …)`; the expected-cancellation
  catch stays silent; `using System.Diagnostics` is removed.
- `AgentManagementViewModel` keeps its existing injected logger and adds the same two local
  catches around its parallel body: silent on surface-local cancellation, `LogError` on the
  unexpected path. (Today its failures reach the runner's generic
  `LogError("Application operation failed: Agent version population")`; the local catch gives
  the correct category and message, and the runner remains as a backstop.)
- Local OCE filters are **required**, not polish: without one, the new page-CTS cancellation
  would escape to the runner and be logged as an Error on every Settings close mid-probe
  (the runner's OCE guard checks only its own token, `ApplicationOperationLifetime.cs:98`).

**Rationale:** injected `ILogger<T>` is what both view-model-side files already use where they
log (`AgentManagementViewModel.cs:30`, legacy `MainWindow.xaml.cs:35`), it is what the logging
spec mandates (`logging/spec.md:69-79`), and it needs no registration change. The static
Serilog `Log` idiom (`PaletteSwitcher`, `SystemThemeFollower`, `ApplicationHost`) is reserved
there for non-DI/composition contexts; both loops are DI-constructed collaborators. Local
categories also disambiguate the two surfaces, which share the operation name
"Agent version population".

### D3: No shared extraction — same shape, not one method

Rejected: a shared "version population engine". The loops differ in item type (`AgentItem` vs
`AgentCardItem`, both `CLIHub` view-model types), per-card steps (availability check, option
rebuild), and write targets. A shared helper would need generics or delegates over those
differences — a new abstraction where the existing pattern (each surface owns its loop) already
fits, and it cannot move to `CLIHub.Core` without relocating WPF-facing observable models.
The consolidation this change delivers is the *shape*: once both loops read identically, a
future extraction (if a third live surface appears) has one pattern to lift.

### D4: Management `Dispose` adopts the pane's cancel-then-flag dispose

`Dispose` bumps the generation, cancels and disposes the CTS (mirroring
`AgentPaneController.Dispose`, `AgentPaneController.cs:144-157`), and still sets `disposed` —
the flag remains load-bearing for the existing `Load`/`DetectAsync` early-returns
(`AgentManagementViewModel.cs:179-182`, `:306-309`).

**Rationale:** this is the "cancellation-on-surface-exit" semantic the pane already has for its
own dispose; for the management page the surface exit is `SettingsWindow.Closed`, and today the
pass survives it. The in-flight probe itself is not (and cannot be) aborted — see the probe
semantics above — which is exactly the pane's behavior.

### D5: Spec delta, not `skip_specs`

A MODIFIED delta on `agent-version`'s "Stale version population does not update the current
list" (original scenario names preserved) extends the requirement with the two genuinely new
durable obligations: an obsolete pass is *cancelled* (not merely ignored) when superseded or
when its surface goes away, and an unexpected pass failure is logged while the list keeps its
state. Two scenarios are added: "Surface exit cancels its population pass" and "Population pass
failure is logged".

**Rationale:** page-exit cancellation and logged failures are observable behavior (background
work stops; errors appear in the log file), not internal plumbing — and the management page's
current compliance with the stale-population requirement is accidental, which the delta turns
explicit. `skip_specs: true` would leave the strengthened contract unrecorded.

### D6: The legacy window is a documented, untouched exception

`MainWindow.xaml.cs` keeps its copy, per the parked remedy. It already has the generation+CTS
shape; its `Debug.WriteLine` stays because the window is never constructed and editing dead
code would create review noise and test obligations for zero users. The class remarks
(`:22-24`) and the resolved backlog entry record the exception.

## Alternatives Considered

### Let the operation-lifetime runner do all failure logging (drop the pane's catches)

**Rejected.** Surface-local cancellation would fall into the runner's generic catch (its OCE
guard tests only the lifetime token) and be logged as an Error on every refresh supersede;
after this change, the same would happen on every Settings close. The two surfaces also share
the operation name "Agent version population", so runner-only logs cannot say which surface
failed.

### Extract one shared population helper

**Rejected** — see D3: different item types and per-card steps; a helper would be a new
abstraction (against `AGENTS.md` "use existing patterns before adding abstractions") and cannot
cross into `CLIHub.Core` without moving WPF-facing view-model types.

### Keep the management loop serial, add only CTS/generation

**Rejected.** The canonical shape includes parallel probing; the service coalesces concurrent
probes anyway (`AgentVersionService.cs:60-64`), serial iteration only delays fill-in. The spec's
per-agent "Versions fill in" scenario is satisfied by both orderings, so parallel is the safe
alignment with the pane.

### Route pane failures through the static Serilog `Log` class

**Rejected.** The static idiom is used by non-DI or composition-root contexts
(`PaletteSwitcher`, `ApplicationHost`); `AgentPaneController` is DI-constructed, the logging
spec's "Service logging integration" expects `ILogger<T>` injection, and injected categories
match what `AgentManagementViewModel` already does.

## Discrepancies

1. **"Two of them swallow exceptions into `Debug.WriteLine`" — true, but only one is live.**
   The swallowers are the pane (`AgentPaneController.cs:186`) and the dead legacy window
   (`MainWindow.xaml.cs:215`). The management loop does **not** swallow: it has no local catch
   and its failures already reach `ILogger` through
   `ApplicationOperationLifetime.RunObservedAsync` (`ApplicationOperationLifetime.cs:102-105`).
   The brief's instruction to "fix the exception-swallowing in BOTH live loops" therefore
   reduces to: pane — replace `Debug.WriteLine` with `LogError`; management — preserve existing
   runner routing behind a new local catch (needed only so its new CTS cancellation is not
   mis-logged as Error by the runner). The plan follows the code, not the phrasing.
2. **"Serial, no cancellation" — confirmed, with a nuance.** The management loop is serial
   (`AgentManagementViewModel.cs:262-277`) and has no surface CTS; it *is* cancellable at
   application shutdown through the `RunAsync` application token. The missing cancellation is
   surface-exit and supersession cancellation, which is what this change adds.
3. **Backlog citations are current.** All three loop citations and the remedy description match
   the tree at `a925d6b`.

## Open Questions

1. **Failure log level.** `LogError` is chosen for unexpected population failures, matching the
   runner's severity for operation failures. If the user prefers quieter diagnostics (these
   failures leave the list at its placeholder rather than breaking the page), `LogWarning`
   is a one-word change per loop — say so before Stage 1.
2. **Cancellation logging.** Surface-local cancellation stays silent (the pane's current
   behavior for supersession). The alternative — one Information line when a page-exit cancel
   stops a pass with work remaining — would aid support at the cost of log noise on every
   Settings close mid-probe. Default chosen: silent.
3. **`tests/CLIHub.Tests/README.md` examples** (`:55`, `:209`) show the `AgentPaneController`
   constructor and will drift by one argument. Task 1.3 updates them mechanically; if the user
   prefers the README untouched, drop that clause.

No question blocks implementation.
