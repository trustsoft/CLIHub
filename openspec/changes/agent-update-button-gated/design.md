# Design: gate Settings card Update button on update availability

## Context (verified current state)

All `file:line` references were verified against the working tree at the current HEAD. `git status --porcelain` shows no unexpected changes before this change directory was created, and `openspec list` reported no active changes before this change was added.

### Current CanUpdate behavior

`AgentCardItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:206-210`) is a settable `bool` property with a backing field. It is set in two places:

1. `AgentManagementViewModel.Load` (`:541`) sets it to `declaresUpdate` (true when the plugin declares an `update` command) at card construction.
2. `AgentManagementViewModel.PopulateBlockedStateAsync` (`:793`) sets it to `declaresUpdate && running.Count == 0` after enumerating running agent processes.

`AgentCardItem.UpdateAvailability` (`:216-227`) is a separate, settable property holding the latest `AgentUpdateAvailability?` result. Its setter raises `PropertyChanged` for `UpdateAvailabilityText` and `HasUpdateAvailable` but not for `CanUpdate`. `HasUpdateAvailable` (`:242`) is a derived property that checks whether `UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable`.

The XAML (`src/CLIHub/Views/SettingsAgentsPage.xaml:290`) binds the Update button's `IsEnabled` to `{Binding CanUpdate}`.

### Availability population

Availability is populated in three paths:

1. Load-time population: `AgentManagementViewModel.PopulateVersionsAsync` (`:698-738`) calls `availability.CheckAsync` for every installed card and assigns `card.UpdateAvailability`.
2. Per-card forced check: `AgentManagementViewModel.CheckCardUpdatesAsync` (`:1013-1059`) invalidates the card's cached result and calls `availability.CheckAsync`, assigning the result to `card.UpdateAvailability`.
3. Header-wide forced check: `AgentManagementViewModel.CheckUpdatesAsync` (`:937-994`) invalidates every installed card's cached result and calls `availability.CheckAsync` for each, assigning results as they arrive.

When a card's agent is not installed, all three paths skip the availability check and leave `UpdateAvailability` at its previous value or null. A probe that reports the agent not installed clears the card's `UpdateAvailability` (`ApplyProbeResult:1075`).

### UpdateCommand and CanExecute

`AgentManagementViewModel.UpdateCommand` (`:115`) is a `RelayCommand<AgentCardItem>` with NO `CanExecute` predicate:

```csharp
this.UpdateCommand = new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent update", ct => this.UpdateAsync(card, ct)));
```

Other card commands (`DetectCardCommand`, `CheckCardUpdatesCommand`) have predicates.

### Card PropertyChanged observation

`AgentManagementViewModel.OnCardPropertyChanged` (`:597-610`) already observes every card's `PropertyChanged` event and responds to `nameof(AgentCardItem.InstallState)` changes by calling `ApplyCardOrdering()`. It returns early unless the property is `InstallState`.

### Post-update availability handling

`AgentManagementViewModel.UpdateAsync` (`:1088-1104`) calls `this.availability.InvalidateFor(card.Plugin)` when the update is started (`:1102`), but it does not assign `card.UpdateAvailability = null`. The cached result is invalidated (the next check will re-probe), but the card's last-assigned availability remains, which would leave `CanUpdate` enabled if it were derived from that state.

### Launch-window Update action

The launch-window Update action is coordinated by `WindowActionCoordinator.CanExecuteUpdate` (`:146-164`). It checks `agent.CanUpdate` (which is declaration-only per `AgentItem.cs:67`) plus a running-agent check using `AgentProcessMatchDetail.ExecutableName` only (`:158`). It does NOT consult availability. The launch-window action remains declaration-based and unchanged per the brief's explicit non-goal.

### Spec overlap and contradictions

- `openspec/specs/agent-ui/spec.md:335-400` — Requirement "Agent card actions" covers the Settings → CLI Agents card actions.
- `openspec/specs/agent-ui/spec.md:219-221` — Requirement "Agent card displayed content" ends with "The update indicator SHALL NOT alter the Update action's availability." and has scenario "Update indicator is advisory" (`:286-290`).
- `openspec/specs/agent-ui/spec.md:9-25` — Requirement "Update command support detection" says `CanUpdate` is true whenever the descriptor declares `update`. This remains true for launch-window `AgentItem.CanUpdate` but becomes false for the gated `AgentCardItem.CanUpdate`.
- `openspec/specs/agent-update-detection/spec.md:110-129` — Requirement "Update availability is advisory" says the result "SHALL NOT gate, disable, enable, or otherwise alter the Update action."
- `openspec/specs/agent-update-detection/spec.md:6` — Purpose says the capability "reports it as advisory information".
- `openspec/specs/preferences-ui/spec.md:621-658` — Requirement "Agent card actions" does not contradict strict gating; it only says Update is unavailable when the agent declares no update command, and that choosing Update runs the command. Deduplication is out of scope.

The `agent-ui` spec is the canonical behavior contract for the Settings → CLI Agents page. All contradictions must be resolved in the delta or by direct edits to the main spec.

### Declaration check API

The correct API to check whether an agent declares an `update` command is:

```csharp
this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null
```

`AgentCommandKind` is in `CLIHub.Core.Models`. `PluginCommandKind` does not exist. Verified at `AgentCardItem.cs:406` and `AgentManagementViewModel.cs:530, 788`.

### Advisory XML comments

The following source comments state that availability is advisory and will need updating:
- `AgentUpdateAvailability.cs:26` — "The result is advisory: it never gates or alters the Update action."
- `IAgentUpdateAvailabilityService.cs:14-15` (interface comment)
- `AgentCardItem.cs:212-215` — "`UpdateAvailability` … is advisory only: it never gates or alters the Update action."
- `AgentCardItem.cs:238-241` — "`HasUpdateAvailable` … Advisory only; it does not enable, disable, or block the Update action."
- `AgentManagementViewModel.cs:517-523` — "Cards render with declarative availability (Update enabled exactly when the plugin declares an update command); the running-agent blocked state populates afterwards…"

## Decisions

### D1 — CanUpdate is a computed property derived from UpdateAvailability, declaration, and running-agent state

**Decision:** `AgentCardItem.CanUpdate` becomes a read-only computed property:

```csharp
public bool CanUpdate =>
    this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable
    && this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null
    && !this.IsBlockedByRunningAgent;
```

A new settable property `IsBlockedByRunningAgent` (type `bool`, default `false`) is added to `AgentCardItem` with XML docs stating it is written by `PopulateBlockedStateAsync` and is part of the Update enablement derivation. The backing field `canUpdate` is removed.

**Rationale:** A computed property makes `CanUpdate` a consistent derivation with one source of truth: the card's current `UpdateAvailability` status, the plugin's declared commands, and the running-agent block. Every write to `UpdateAvailability` or `IsBlockedByRunningAgent` automatically re-computes the value and raises `PropertyChanged`, so the XAML binding updates without manual synchronization. The running-agent state is included in the derivation to preserve the existing gating behavior (`PopulateBlockedStateAsync:793` writes `declaresUpdate && running.Count == 0`). The pass-currency concern is already handled by `blockedStatePass.IsCurrent` (`:791`) and by new cards defaulting to false (the same transient state that exists today).

**Rejected alternative (from original D2) — exclude running-agent state:** would drop the existing `PopulateBlockedStateAsync:793` gating and cause a behavior regression the user did not ask for. The spec delta says "Update" is available only when availability is `UpdateAvailable` AND declaration AND **no matching agent process is running**. Including the running-agent state in the derivation preserves that gate.

### D2 — PopulateBlockedStateAsync writes IsBlockedByRunningAgent instead of CanUpdate

**Decision:** `AgentManagementViewModel.PopulateBlockedStateAsync` (`:788-810`) is changed to write `card.IsBlockedByRunningAgent = running.Count > 0` instead of `card.CanUpdate = declaresUpdate && running.Count == 0` (`:793`). The block message behavior (`:797-800`) is unchanged.

**Rationale:** `CanUpdate` is now computed, so explicit writes would not compile. The pass already guards with `blockedStatePass.IsCurrent` (`:791`), so stale results are not applied. Writing `IsBlockedByRunningAgent` updates one input to the computed `CanUpdate`, which raises `PropertyChanged` through the `IsBlockedByRunningAgent` setter.

### D3 — UpdateAvailability and IsBlockedByRunningAgent setters raise PropertyChanged for CanUpdate

**Decision:** `AgentCardItem.UpdateAvailability` setter (`:216-227`) is extended to raise `OnPropertyChanged(nameof(CanUpdate))` after it raises for `UpdateAvailabilityText` and `HasUpdateAvailable`. The new `IsBlockedByRunningAgent` setter raises `OnPropertyChanged(nameof(CanUpdate))` after setting the backing field.

**Rationale:** `CanUpdate` derives from both `UpdateAvailability.Status` and `IsBlockedByRunningAgent`, so every write to either input changes the computed value (or keeps it the same, which is a no-op raise). The XAML `IsEnabled` binding reads `CanUpdate`, so it must receive `PropertyChanged` whenever the derivation inputs change.

### D4 — UpdateCommand gains a CanExecute predicate and OnCardPropertyChanged raises it

**Decision:** `AgentManagementViewModel.UpdateCommand` (`:115`) is changed from a `RelayCommand<AgentCardItem>` with no predicate to one with a `CanExecute` predicate:

```csharp
this.UpdateCommand = new RelayCommand<AgentCardItem>(
    card => _ = this.RunAsync("Agent update", ct => this.UpdateAsync(card, ct)),
    card => card?.CanUpdate == true);
```

`AgentManagementViewModel.OnCardPropertyChanged` (`:597-610`) is extended to respond to `nameof(AgentCardItem.CanUpdate)` changes by calling `this.UpdateCommand.RaiseCanExecuteChanged()` (it currently returns early unless the property is `InstallState`).

**Rationale:** The XAML button binding reads `CanUpdate`, but WPF command execution also consults the command's `CanExecute`. A command without a predicate is always executable (unless the binding itself disables the button). Adding the predicate mirrors the button enablement and ensures the command cannot be invoked when the card is not eligible. `OnCardPropertyChanged` already observes every card and is the correct place to raise `CanExecuteChanged` when `CanUpdate` changes; `RaiseBusyStateChanges` is for header-wide busy state, not per-card property changes.

**Rejected alternative (from original D4) — raise in RaiseBusyStateChanges:** that method (`:661-677`) is for header-wide actions (`DetectCommand`, `CheckUpdatesCommand`), not per-card state. `OnCardPropertyChanged` already observes cards and is the correct trigger point.

### D5 — UpdateAsync clears the card's UpdateAvailability when the update starts

**Decision:** `AgentManagementViewModel.UpdateAsync` (`:1088-1104`) is extended to assign `card.UpdateAvailability = null` when `result.Started` is true, before invalidating the cached result (`:1102`).

**Rationale:** The cached result is invalidated (`:1102`) so the next check will re-probe, but the card's last-assigned availability remains, which would leave `CanUpdate` enabled (because `UpdateAvailability` is non-null with `Status == UpdateAvailable`). Clearing the card's availability to null makes `CanUpdate` false immediately, so the button disables until the next availability check resolves a new result. This matches the brief's requirement: "what should happen to availability (and therefore the button) after an update is started."

**Alternative considered:** leave the availability as-is and rely on the invalidation. Rejected because the card would show the stale "update available" indicator and the button would stay enabled until the next check runs (which may be delayed by the cache TTL). Clearing it immediately gives the user faster feedback.

### D6 — Load no longer writes CanUpdate

**Decision:** `AgentManagementViewModel.Load` (`:541`) no longer sets `card.CanUpdate`. The property is read-only and computed.

**Rationale:** The property is now derived from `UpdateAvailability`, `IsBlockedByRunningAgent`, and the plugin's commands, so explicit writes are not needed and would not compile. The derivation is automatic on every input change.

### D7 — preferences-ui overlap is left unchanged; deduplication is out of scope

**Decision:** The `openspec/specs/preferences-ui/spec.md:621-658` requirement "Agent card actions" is left unchanged. No delta for `preferences-ui` is written.

**Rationale:** The `preferences-ui` requirement does not contradict strict gating. It only says Update is unavailable when the agent declares no update command, and that choosing Update runs the command. Both remain true. Full deduplication (removing or deferring that requirement to `agent-ui`) is out of scope for this change, which focuses on gating the Settings card Update action. The `agent-ui` spec remains the canonical detailed contract for Settings → CLI Agents card actions.

### D8 — Spec deltas resolve all contradictions

**Decision:** The `agent-ui` delta includes three requirement modifications:

1. MODIFIED "Agent card actions" (`:335-464`) — replaces the entire requirement with strict gating (availability AND declaration AND no running agent).
2. MODIFIED "Agent card displayed content" (`:219-303`) — changes the sentence "The update indicator SHALL NOT alter the Update action's availability" to state that the indicator and Update enablement come from the same availability result, and rewrites the body of scenario "Update indicator is advisory" (`:286-290`) to the gated behavior. The scenario name is retained because `openspec validate` refuses a MODIFIED block that omits an existing scenario name; the name is now historical.
3. MODIFIED "Update command support detection" (`:9-25`) — scopes declaration detection to the launch-window model (`AgentItem.CanUpdate` remains true whenever the descriptor declares `update`) and points to "Agent card actions" for the Settings card rule (`AgentCardItem.CanUpdate` now gated).

The `agent-update-detection` delta removes the "Update availability is advisory" requirement (`:110-129`) and adds a new requirement stating that availability gates the Settings card action. The Purpose line at `:6` ("reports it as advisory information") is edited directly in the main spec `openspec/specs/agent-update-detection/spec.md` as a one-line contract correction (not a new capability; a delta `## Purpose` is ignored for existing capabilities per the validator).

**Rationale:** These are the exact contradictions that would survive after archiving. All must be resolved so the main specs accurately reflect the gated behavior after this change is merged.

**Scenario name note:** The scenario "Update indicator is advisory" (`:286-290`) keeps its name because the validator requires a MODIFIED requirement to carry every existing scenario name, but its WHEN/THEN body now states the gated behavior (indicator shown and Update available only for `UpdateAvailable`; both absent for `Unknown`/`UpToDate`). The name is historical and does not describe the new behavior.

## Deviations from the brief

1. **Running-agent state included in CanUpdate derivation (D1, contradicting original D2).** The brief's design question (a) asked "how to make CanUpdate consistent" but did not specify whether to include running-agent state. The orchestrator finding #2 pointed out that the spec delta says "AND no matching agent process is running," and that dropping the existing `PopulateBlockedStateAsync:793` gating would be a regression the user did not ask for. D1 now includes `IsBlockedByRunningAgent` in the derivation to preserve the existing gating behavior.

2. **OnCardPropertyChanged raises UpdateCommand.CanExecuteChanged (D4, not RaiseBusyStateChanges).** The orchestrator finding #3 corrected the raise location: `OnCardPropertyChanged` already observes card changes and is the correct place to raise when `CanUpdate` changes. `RaiseBusyStateChanges` is for header-wide busy state.

3. **preferences-ui left unchanged (D7, contradicting original plan).** The orchestrator finding #4 noted that the `preferences-ui` requirement does not contradict strict gating and deduplication is out of scope. The delta for `preferences-ui` is removed.

4. **Three agent-ui requirement modifications (D8, not one).** The orchestrator finding #5 identified two additional contradictions in `agent-ui` (the "Agent card displayed content" advisory language and the "Update command support detection" scope) that must be resolved. D8 adds two more MODIFIED requirements.

5. **Purpose edit in main spec (D8).** The orchestrator finding #5 noted that a delta `## Purpose` is ignored for existing capabilities. The Purpose line is edited directly in the main spec as part of this change.

6. **Launch-window claim corrected (context section).** The orchestrator finding #6 corrected the false claim that `WindowActionCoordinator` "already consults availability". The context now states it is declaration-based and unchanged.

## Open questions

1. **Availability clearing vs. reporting Unknown.** D5 clears `UpdateAvailability` to null when an update starts. An alternative is to assign `Unknown` status instead. Clearing to null is simpler (no new status construction) and matches the card state before the first check. If the implementation finds that null causes confusion (no indicator vs. "update unavailable"), assign `Unknown` instead.

2. **Test determinism.** The load-time availability population is fire-and-forget (`:698-738` `PopulateVersionsAsync`); tests that assert `CanUpdate` after `Load` must wait for the pass using `SpinWait.SpinUntil`. Existing tests demonstrate the pattern (`AgentManagementViewModelTests.Load.cs:419`, `.UpdateAvailability.cs:796`). New tests must follow that pattern.

## Implementation notes

### Implemented as designed

- `AgentCardItem.CanUpdate` is a computed read-only property over `UpdateAvailability`, the declared
  `update` command (`this.Plugin.Commands?.Get(AgentCommandKind.Update)`), and the new
  `IsBlockedByRunningAgent` flag. `Load` and `PopulateBlockedStateAsync` write only that flag / the
  availability result; the `UpdateAvailability` and `IsBlockedByRunningAgent` setters raise
  `PropertyChanged` for `CanUpdate`.
- `UpdateCommand` gained `card => card?.CanUpdate == true`; `OnCardPropertyChanged` raises
  `UpdateCommand.RaiseCanExecuteChanged()` on `CanUpdate` changes and still applies card ordering on
  settled `InstallState` changes.
- `UpdateAsync` clears `card.UpdateAvailability` before `InvalidateFor` when the update starts.
- D5's open question 1 is resolved as specified: availability is cleared to null rather than replaced
  with `Unknown`; tests confirm no indicator appears and Update stays disabled until the next result.

### Clarifications and deviations

1. **Scenario-count wording in task 8.1 is inaccurate, behavior is intact.** The main `agent-ui`
   "Agent card actions" requirement carries 21 scenarios, not 27. The delta reproduces all 21 by name
   and adds 5 gated scenarios (26 total); "Agent card displayed content" reproduces all 14;
   "Update command support detection" reproduces both originals and adds "Settings card CanUpdate is
   gated". No existing scenario was dropped.
2. **Additional advisory-comment corrections.** Beyond the five lines listed in Stage 4, three more
   comments still asserted the result was advisory: the `IAgentUpdateAvailabilityService` summary
   ("Results are advisory only"), the `AgentManagementViewModel` constructor parameter
   ("Advisory update availability service"), and `AgentCardItem.UpdateAvailabilityText`
   ("The advisory update indicator text"). They were reworded to describe the gated Settings card
   behavior; no behavior changed.
3. **Extra test for the new "Update clears availability when started" scenario.**
   `UpdateCommand_WhenStarted_ClearsUpdateAvailability` was added to the new
   `AgentManagementViewModelTests.UpdateCommand.cs` file so the added scenario is falsifiable; Stage 6
   did not enumerate it.
4. **Two test names updated to match the gated rule.** `Load_WithNoRunningProcesses_KeepsDeclaredUpdateAvailableWithoutMessage`
   became `...KeepsGatedUpdateAvailableWithoutMessage`, and `Load_WhenEnumerationFails_KeepsDeclaredAvailability`
   became `...KeepsGatedAvailability`. `Load_ShowsDeclarativeAvailability_BeforeBlockedStateLands` keeps
   its historical name (it still verifies that Update is enabled from availability before the
   blocked-state pass lands) but now drives availability to `UpdateAvailable` first.
5. **Existing load-time tests now drive availability instead of asserting declaration enablement.**
   Tests that expected `CanUpdate` to be true after `Load` without an availability result were not
   weakened: each now mocks `CheckAsync` to return `UpdateAvailable` for the declaring card and waits
   for the result (and, where relevant, for the blocked-state pass), then asserts the gated outcome.
   The load-time population is synchronous under the suite's `InlineOperationLifetime`; `SpinWait` is
   used where the plan specified it and in the deferred-lifetime scenarios.
6. **Independent verification follow-ups.** The verifier found one stale contract statement
   (`docs/architecture/plugins.md:43`, "The result never gates or alters the Update action"), which now
   says the Settings card action is gated while the launch window stays declaration-based. The two
   retained detection scenarios were qualified as launch-window (`AgentItem`) checks so they cannot be
   read as assertions about `AgentCardItem.CanUpdate`, and the scenario count in task 8.1 was corrected
   to 21. The verifier's remaining nit — `CanUpdate` does not itself test `IsInstalled` and relies on
   every caller clearing availability for a not-installed card — was parked in `docs/backlog.md` rather
   than widening this change; no reachable wrong state exists today.

