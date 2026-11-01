## Context

See `proposal.md`. All evidence below is verified against
`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` at HEAD `f963dba` (line numbers re-checked
during this investigation; the brief's numbers drifted by at most a few lines and are corrected here).

### Current state: `DetectAsync` (`:617-664`)

```csharp
private async Task DetectAsync(CancellationToken cancellationToken)
{
    this.IsDetecting = true;
    try
    {
        var cards = this.Cards.ToList();
        var previousStates = cards.ToDictionary(card => card, card => card.InstallState);   // :623

        foreach (var card in cards)                                                          // :627-630
        {
            card.InstallState = AgentInstallState.Detecting;
        }

        try
        {
            foreach (var card in cards)                                                      // :634-643
            {
                var result = await this.probe.ProbeAsync(card.Plugin, cancellationToken);
                if (this.disposed)                                                            // :637-640
                {
                    return;                                                                   // <-- BUG 2: skips the catch's restore
                }

                this.ApplyProbeResult(card, result);                                          // :642
            }

            this.RefreshAvailabilitySummary();
            this.RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id);
        }
        catch (Exception)
        {
            foreach (var card in cards)                                                       // :652-655
            {
                card.InstallState = previousStates[card];                                     // <-- BUG 1: only InstallState restored
            }

            throw;
        }
    }
    finally
    {
        this.IsDetecting = false;
    }
}
```

- **Bug 1 (partial restore).** `previousStates` (`:623`) is `Dictionary<AgentCardItem, AgentInstallState>`
  — it captures only the enum, not `Version` or `UpdateAvailability`. `ApplyProbeResult` (`:785-798`)
  writes `InstallState` together with `Version` (and clears `UpdateAvailability` on a not-installed
  result) as one atomic-looking unit:

  ```csharp
  private void ApplyProbeResult(AgentCardItem card, AgentProbeResult result)
  {
      if (result.IsInstalled)
      {
          card.InstallState = AgentInstallState.Installed;
          card.Version = result.Version;
      }
      else
      {
          card.InstallState = AgentInstallState.NotInstalled;
          card.Version = null;
          card.UpdateAvailability = null;
      }
  }
  ```

  If card A's probe succeeds and writes `(Installed, "2.0", <unchanged>)`, then card B's probe
  throws, the catch sets *every* card's `InstallState` back to its pre-pass value — including card
  A, whose `InstallState` reverts to (say) `NotInstalled` while its `Version` stays `"2.0"` (written
  moments earlier and never rolled back). `DefaultAgentDetail` (`:126-143`) reads only `Version`:

  ```csharp
  var version = this.Cards.FirstOrDefault(card => card.PluginId == this.CurrentDefaultAgentId)?.Version;
  return version is null ? "Preselected in the launch window" : $"Version {version}";
  ```

  So the default-agent summary can read `"Version 2.0"` for a card the selector
  (`AgentOption.DisplayName`, `AgentCardItem.cs:49-54`) renders `"{Name} (Not installed)"` — an
  internally inconsistent `(InstallState, Version)` pair, confirmed reachable through this exact
  path.

- **Bug 2 (disposed early return skips restore).** The `if (this.disposed) { return; }` at
  `:637-640` exits the `async Task` directly. A C# `return` inside a `try` block still runs that
  try's `finally` (here, `this.IsDetecting = false;` at `:661`), but it does **not** enter the
  `catch` block — `catch` only runs for a propagating exception, never for a `return`. So every card
  the pass had not yet reached when `this.disposed` flipped true keeps `InstallState = Detecting`
  (written at `:627-630`) with no further write ever landing on it (the view model is disposed; no
  later pass will revisit these specific card instances unless `Load()` rebuilds the list from
  scratch, which replaces the cards entirely rather than fixing the stuck ones in place). This is a
  logic bug independent of severity: the proposal's non-goal section correctly notes the *practical*
  blast radius is small (the surface is being torn down), but the code path itself does not settle
  the card, which is what this change's invariant requires.

### Current state: `CheckUpdatesAsync` (`:666-712`)

```csharp
private async Task CheckUpdatesAsync(CancellationToken cancellationToken)
{
    this.IsCheckingUpdates = true;
    try
    {
        this.UpdateCheckSummary = null;                                                       // :673

        var cards = this.Cards.ToList();
        var checkedCount = cards.Count(card => card.InstallState == AgentInstallState.Installed);

        await Task.WhenAll(cards.Select(async card =>                                         // :681-698
        {
            if (card.InstallState != AgentInstallState.Installed)
            {
                card.UpdateAvailability = null;
                return;
            }

            this.availability.InvalidateFor(card.Plugin);
            var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
            if (!this.disposed)
            {
                card.UpdateAvailability = availability;
            }
        }));

        if (!this.disposed)                                                                   // :700-706
        {
            this.UpdateCheckSummary = BuildUpdateCheckSummary(checkedCount, cards.Count(card => card.HasUpdateAvailable));
            this.RefreshAvailabilitySummary();
        }
    }
    finally
    {
        this.IsCheckingUpdates = false;
    }
}
```

- There is **no `catch`** around the `Task.WhenAll`. If any one card's `CheckAsync` call throws
  (not a graceful `Unknown` result — an actual thrown exception, e.g. the mock-driven
  `ThrowsAsync` pattern already used elsewhere in this test suite for `IAgentProbeService`), the
  exception propagates out of `CheckUpdatesAsync` itself. `this.UpdateCheckSummary` stays `null`
  (set at `:673`, never reached again) while `this.IsCheckingUpdates` resets to `false` through the
  `finally` (`:708-711`), so the header action becomes available again — but the muted summary line
  is simply gone. `BuildUpdateCheckSummary` (`:761-775`) never itself returns `null` or an empty
  string; its only outputs are `"No installed agents to check"`, `"Checked {n} agent(s) · no
  updates"`, or `"Checked {n} agent(s) · {m} update(s) available"`. A `null` `UpdateCheckSummary` is
  bound by `IAgentManagementSection.UpdateCheckSummary` (`get; private set`, `:289-293`) and rendered
  as an absent/empty line by the XAML consumer — there is no dedicated "no updates" literal to
  confuse it with textually, but the *absence* of any summary after a user-triggered "Check updates"
  click reads as "nothing to report," which is the ambiguity the brief and the backlog entry both
  flag. Some cards may carry a fresh `UpdateAvailability` (those whose `Task.WhenAll` branch
  completed before the failing one), while others keep their pre-pass (possibly stale) indicator —
  this part is benign: `InvalidateFor` already dropped the cache for every card whose branch ran
  far enough to call it, so the *next* check re-fetches correctly; only the *summary line* is wrong
  for the failed pass.

### Current state: single-card actions (`DetectCardAsync`, `CheckCardUpdatesAsync`) — confirmed correct, out of scope

- `DetectCardAsync` (`:590-615`): snapshots `var previousState = card.InstallState;` (one card, one
  field) before flipping to `Detecting`, and its `catch` restores exactly that field then
  rethrows. Since a single-card pass only ever touches that one card, and `ApplyProbeResult` is
  only called in the success path (never reached before the restore on a throw), there is no
  partial-settle hazard: the card either fully commits (`ApplyProbeResult`, then the message/summary
  writes) or fully reverts (`InstallState` back to its only pre-pass value — `Version` and
  `UpdateAvailability` were never touched because `ApplyProbeResult` never ran). Confirmed by the
  existing test `DetectCardCommand_WhenProbeThrows_RestoresPreviousCardState`
  (`AgentManagementViewModelTests.Detection.cs:180-201`).
- `CheckCardUpdatesAsync` (`:722-752`): no explicit try/catch, but also no mutation before the
  single `await this.availability.CheckAsync(...)` call — `InvalidateFor` has no card-visible
  effect, and the `if (!this.disposed) { card.UpdateAvailability = ...; }` write only happens after
  the await returns. A thrown `CheckAsync` propagates with the card's prior `UpdateAvailability`
  untouched. Confirmed by `CheckCardUpdatesCommand_WhenCheckThrows_RestoresGatesAfterFailure`
  (`AgentManagementViewModelTests.UpdateAvailability.cs:707-737`), which already asserts
  `Assert.Equal(beforeFailure, card.UpdateAvailability);` and `Assert.Null(card.Message);`.
- Both single-card actions run inside a `CardActionScope` (`:840-873`) whose `finally`-equivalent
  `Dispose()` always resets the per-card busy flag and the running-action counter regardless of how
  the action ends — already correct and untouched by this change.

## Goals / Non-Goals

**Goals:**

- One invariant, enforced identically regardless of why a header-wide pass stopped early (thrown
  probe/check, or `this.disposed` becoming true mid-pass): no card is left in `Detecting`, and no
  card is left with an `(InstallState, Version, UpdateAvailability)` combination that
  `ApplyProbeResult`/settle logic would never itself produce.
- `CheckUpdatesAsync` never leaves `UpdateCheckSummary` as a cleared `null` after a failed pass; it
  always ends in either a real `BuildUpdateCheckSummary` result or an explicit failure summary.
- Reuse one shared settle primitive between `DetectAsync`'s two abort paths (exception, disposed
  early return) rather than writing the disposed-path fix as a second hand-rolled loop.
- Never roll back a card a pass has already committed a fresh result to. A fresh probe/check result
  is truthful data about the host right now; discarding it to match stale pre-pass state would be
  actively wrong, not merely unnecessary.

**Non-Goals:** see `proposal.md`'s Non-Goals section (verbatim; not repeated here).

## Decisions

### 1. Track "resolved" cards per pass instead of widening the snapshot dictionary alone

**Decision:** `DetectAsync` builds a `HashSet<AgentCardItem> resolved` alongside the existing
`previousStates` snapshot (which is widened to capture `Version` and `UpdateAvailability`, not just
`InstallState`). Immediately after each successful `ApplyProbeResult(card, result)` call, add that
card to `resolved`. The settle routine — run from both the `catch` and the (rewritten) disposed
early-return path — iterates `cards` and, for every card **not** in `resolved`, restores the full
pre-pass triple from `previousStates`; cards already in `resolved` are left untouched.

**Rationale:** This directly encodes "a card that already settled during the pass keeps its fresh,
truthful result; only cards still mid-flight (still showing the pass's `Detecting` announcement)
get rolled back." It requires no change to `ApplyProbeResult`'s signature or behavior.

**Alternative rejected — roll back every card unconditionally (current behavior, widened to all
three fields):** Simpler (one dictionary, no set), but actively discards a fresh, correct probe
result for a card that successfully resolved moments before a *different* card's probe threw. The
brief's own decision explicitly rules this out ("do not roll back cards that already settled during
the pass").

**Alternative rejected — snapshot a full `AgentCardItem` clone/record per card:** Equivalent
information content to the `(InstallState, Version, UpdateAvailability)` tuple actually at risk, but
adds an unused clone of every other property (`Message`, `IsDetecting`, etc.) that the restore never
touches. A narrow three-field record (or named tuple) is the smaller, more legible change.

### 2. Share one settle routine between the exception path and the disposed early return

**Decision:** Extract a private helper, e.g.
`SettleAbortedDetectPass(IReadOnlyList<AgentCardItem> cards, HashSet<AgentCardItem> resolved, Dictionary<AgentCardItem, (AgentInstallState InstallState, string? Version, AgentUpdateAvailability? UpdateAvailability)> previousStates)`,
that performs exactly the "restore unresolved cards" loop. Call it from the `catch (Exception)`
block (before `throw;`) and from the disposed-early-return branch (before `return;`), replacing the
inline loop at `:652-655` and the bare `return;` at `:639`.

**Rationale:** This is the brief's explicit instruction ("prefer ONE settle primitive shared by the
exception path and the disposed early return, not two hand-written loops"). It also means a future
third abort path (if one is ever added) has one obvious place to call into, rather than a third copy
of the loop.

**Alternative rejected — convert the disposed check into a thrown `OperationCanceledException`:**
Would let the existing `catch` handle both paths with zero new code, but `OperationCanceledException`
already has a specific, different meaning in this codebase (see `PopulateVersionsAsync`'s
`catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` at `:523-525`)
and callers/tests that distinguish "cancelled" from "disposed mid-pass" would become harder to
reason about. A dedicated settle helper keeps the disposed check's semantics (an early, deliberate
stop, not a cancellation token firing) intact while still sharing the restore logic.

### 3. `CheckUpdatesAsync` gets a `catch` that sets an explicit failure summary

**Decision:** Wrap the `await Task.WhenAll(...)` call (and the subsequent summary-build block) in a
`try`/`catch (Exception)`. On catch, if `!this.disposed`, set
`this.UpdateCheckSummary = "Update check failed";` and call `this.RefreshAvailabilitySummary();`
(so the UPDATES card's derived properties, which also read per-card `UpdateAvailability`, stay in
sync with whatever subset of cards did complete before the throw), then rethrow so the caller's
existing operation-lifetime logging (`ApplicationOperationLifetime`, "Application operation failed")
still fires exactly as it does today for every other operation failure in this file. If
`this.disposed`, skip both writes (mirrors the existing `if (!this.disposed)` guard pattern at
`:700` and `:738`) since there is no card view to update.

**Rationale:** Directly implements the brief's recommended remedy and the matching backlog proposal
(`docs/backlog.md:181-183`: *"set a short failure summary (for example 'Update check failed') so the
empty line cannot read as 'no updates'"*). Rethrowing (rather than swallowing) preserves the
existing failure-is-logged contract exercised by every other `RunAsync`-wrapped operation in this
view model; `CheckUpdatesAsync` is always invoked through
`this.RunAsync("Agent update check", this.CheckUpdatesAsync)` (`:81`), so letting the exception
continue to propagate out of the delegate keeps the existing `ApplicationOperationLifetime` logging
behavior unchanged — this change touches only what the UI shows, never whether or how the failure is
logged.

**Alternative rejected — restore every card's pre-pass `UpdateAvailability` on failure:** The brief
explicitly asks to record why this was rejected. Per-card results are independent and already
individually invalidate-then-check (`InvalidateFor` then `CheckAsync`, `:692-693`); a card whose
check already completed successfully before a *different* card's check threw has a fresh, correct
result — rolling it back would re-introduce the exact "discard truthful fresh data" problem Decision
1 avoids for `DetectAsync`, for no benefit (the summary line, not the per-card indicators, is the
actual defect). The existing `Task.WhenAll` semantics already leave not-yet-completed cards with
their untouched pre-pass indicator, which is correct (stale-but-honest, and self-heals on the next
check since the cache was already invalidated for every card whose branch got far enough to call
`InvalidateFor`).

**Alternative considered and partially adopted — swallow the exception instead of rethrowing:**
Would make the failure summary the only signal, with no corresponding `ApplicationOperationLifetime`
log entry. Rejected: every other header/card action in this file lets an unexpected exception
propagate to the shared logging boundary; silently swallowing here would make `CheckUpdatesAsync`
the only inconsistent case and would hide a real failure from logs/diagnostics. The failure summary
and the log entry are not mutually exclusive — do both.

### 4. Capability spec ownership: `agent-detection` and `agent-update-detection`, not `agent-availability-display` or `agent-ui`

**Decision:** Place the `DetectAsync` settle-consistency guarantee under `agent-detection`'s existing
"On-demand detection re-evaluation" requirement (new scenarios), and the `CheckUpdatesAsync`
failure-summary guarantee under `agent-update-detection`'s existing "On-demand update availability
check" requirement (new scenario). No delta to `agent-ui` or `agent-availability-display`.

**Rationale:**

- `openspec/specs/agent-detection/spec.md`'s "On-demand detection re-evaluation" requirement already
  owns the header Detect pass's probe-and-report contract (scenarios "Manual probe bypasses the
  cache", "Probe reports installed/not installed") — this is exactly the pass whose abort behavior
  is being fixed. It is the natural home for "a probe that throws, or a pass that is torn down
  mid-flight, must still leave every card in a valid state."
- `openspec/specs/agent-update-detection/spec.md`'s "On-demand update availability check" requirement
  already owns the forced-check contract this view model implements (`CheckUpdatesCommand` /
  `CheckUpdatesAsync`), including its existing caching/degrade-to-`Unknown` guarantees. A failed pass
  producing a trustworthy summary is a natural extension of "the forced check... assigns each
  checked agent's fresh result to its card" — it is still describing what the forced check
  guarantees about its own outcome, just for the failure case.
- `agent-ui` (`openspec/specs/agent-ui/spec.md`) already carries the *display* contract for the
  existing, correctly-working parts of this behavior: "Detecting never persists after a failed
  probe" (under "Agent card displayed content", card-level) and "Check updates reports a summary" /
  "Header actions become available again when a pass completes" (under "Agent management header
  actions"). These describe what the user sees on the card/header, assuming the underlying service
  methods behave correctly. This change is a *service-method-level* correctness fix (what
  `DetectAsync`/`CheckUpdatesAsync` write to the model), not a new display rule — the existing
  `agent-ui` scenarios already promise the right *outward* behavior; what was missing was the
  `AgentManagementViewModel` code actually keeping that promise in the multi-card, mid-pass-abort
  case. Changing `agent-ui` itself would duplicate intent already captured by `agent-detection`
  and `agent-update-detection`, and risks drifting the display contract from the detection/update
  contracts it is supposed to assume.
- `agent-availability-display` (dimming/filtering by project availability) is unrelated — it governs
  presentation of agents not available in the *current project*, not the host-installation/update
  pass lifecycle. Not touched.

**Alternative rejected — put both deltas under `agent-ui`:** `agent-ui`'s "Detecting never persists
after a failed probe" scenario (verified at `openspec/specs/agent-ui/spec.md:246-249`) is a
card-level, single-probe statement; the brief requires this scenario's *name* and intent to be
preserved unchanged, which argues for leaving it alone and adding the multi-card/pass-level
guarantee where the pass's own contract lives (`agent-detection`/`agent-update-detection`), not by
overloading the existing card-level scenario with pass-wide semantics it was not written to carry.

### 5. `MODIFIED` vs `ADDED` requirement choice in each delta

**Decision:** `agent-detection`'s "On-demand detection re-evaluation" requirement body gains one
added sentence (the settle guarantee) and keeps every existing scenario name; new scenarios are
appended. Same shape for `agent-update-detection`'s "On-demand update availability check" —
`## MODIFIED Requirements` in both delta files, matching the project's established convention (see
`openspec/changes/archive/2026-10-27-independent-card-actions/specs/agent-ui/spec.md` and
`openspec/changes/archive/2026-10-20-harden-probe-caching-and-process-matching/specs/agent-detection/spec.md`
for the precedent of widening a requirement body while preserving every prior scenario name
verbatim).

## Risks / Trade-offs

- **[Risk] A future reviewer expects the disposed-early-return fix to use cancellation instead of a
  dedicated branch.** → Documented as Decision 2's rejected alternative; the dispose flag's existing
  meaning in this file is deliberately distinct from `OperationCanceledException` and is left as-is.
- **[Risk] `CheckUpdatesAsync`'s rethrow means the failure summary and the operation-lifetime log
  both fire for the same failure — looks like double reporting.** → This matches the existing
  pattern for every other operation in the file (a result is set/left, then the exception still
  propagates to the shared log boundary); not a new inconsistency introduced by this change.
- **[Risk] Widening `previousStates` to a 3-field snapshot per card for every `DetectAsync` pass adds
  a small allocation on every detect.** → Negligible: the existing code already allocates a
  dictionary keyed by every card for the 1-field version; widening the value type adds no new
  allocation shape, just more fields copied per entry.

## Migration Plan

1. Widen `DetectAsync`'s snapshot to a 3-field value per card; add the `resolved` tracking set;
   extract the shared settle helper; call it from both the `catch` and the disposed early-return
   path.
2. Add a `try`/`catch` around `CheckUpdatesAsync`'s check-and-summarize block; on catch (and not
   disposed) set the explicit failure summary and refresh the aggregation properties, then rethrow.
3. Extend the existing `DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState` test with a
   card that settles before the throw; add new tests for the `DetectAsync` disposed-mid-pass case
   and the `CheckUpdatesAsync` thrown-mid-pass case.
4. Run the full build/test/validate gates; update the three `docs/backlog.md` entries (lines
   141-157, 159-170, 172-186) to `[done]` with the implementing change's id, per the file's own
   convention — this falls to the implementation session, not this planning pass (planning makes no
   source or backlog edits).

## Open Questions

- **Exact wording of the failure summary text.** The brief and the backlog entry both suggest
  `"Update check failed"` as an example, not a mandate. The implementation session should pick exact
  wording consistent with `BuildUpdateCheckSummary`'s existing tone (`"Checked {n} agents · ..."`)
  and should add that literal to the `agent-update-detection` spec scenario precisely, not leave it
  implicit — left open here because the brief frames it as "recommended," not fixed.
- **Whether `DetectAsync`'s settle helper should also be reused by `PopulateVersionsAsync`/
  `PopulateBlockedStateAsync`.** Those two load-time passes use a different mechanism entirely
  (`RestartablePass` generation guards, which make a stale write a no-op rather than something to
  roll back) and are explicitly out of scope per the proposal's Non-Goals. Flagging here only so the
  implementation session does not accidentally widen scope while the new helper is fresh in view.
- **Whether the new `DetectAsync` tests should also assert the `DefaultAgentOptions` /
  `DefaultAgentDetail` consistency the proposal's Why section calls out**, or whether asserting
  `Version`/`UpdateAvailability`/`InstallState` directly on the `AgentCardItem` is sufficient
  evidence of the fix. Recommend the implementation session add at least one assertion through
  `DefaultAgentDetail` or `AgentOption.DisplayName` since that is the concretely observed symptom in
  the brief, but this plan does not mandate the exact assertion shape.
- **Deviation note:** the brief's line numbers (`:623`, `:637-640`, `:648-657`, `:666-712`, `:785-798`)
  were each within 1-3 lines of this investigation's re-verified numbers (`:623`, `:637-640`,
  `:652-655`+`:657`/`:648`, `:666-712`, `:785-798`) — no material drift, no contradiction between the
  brief and the code found. No deviation from the brief was required.

## Implementation addendum (2026-11-01)

Two fixes were added during verification of the implementation; the plan above did not anticipate
them. Both are recorded here because they extend the change's shipped behavior.

1. **Aborted `DetectAsync` rebuilds the derived surfaces.** `DefaultAgentDetail` reads `Cards` live,
   but `DefaultAgentOptions` were rebuilt only on the success path. After an aborted pass a card the
   pass had settled as installed therefore still had a selector option built at `Load` reading
   "(Not installed)", while the summary card showed "Version 2.0" — the misleading pairing the
   proposal's Why-item 1 describes, with the stale half on the selector side. The abort path now
   calls `RefreshAvailabilitySummary()` and `RebuildDefaultAgentOptions(...)` after settling, guarded
   by `!this.disposed`; the disposed branch deliberately does not, because that surface is gone.
   Covered by the extended `DetectCommand_WhenProbeThrowsAfterACardSettled_...` test.
2. **Cancellation is not a failure.** The new `catch` in `CheckUpdatesAsync` also caught
   `OperationCanceledException`, so a check cancelled by application shutdown would have shown
   "Update check failed" while the operation boundary logged it as cancelled. The catch is now
   filtered with `when (ex is not OperationCanceledException)`; the exception still propagates and
   the summary is left untouched. Covered by
   `CheckUpdatesCommand_WhenCheckIsCancelled_LeavesNoFailureSummary`.

Independent verification also corrected one scenario in the `agent-detection` delta: a probe reports
only installation state and version, never update availability, so the scenario now says the settled
card keeps its fresh installation state and version and an update-availability indicator consistent
with that state. The requirement body was already accurate.
