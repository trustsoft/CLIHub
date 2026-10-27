# Independent per-agent card actions on Settings → CLI Agents

## Why

A user reported a regression on the per-agent cards of Settings → CLI Agents: starting the per-card
"Check updates" on any one agent greys out the per-card "Detect" and "Check updates" buttons on
**every other** agent card. The user expects each agent's card actions to be independent — checking
or detecting one agent must not disable another agent's card actions.

The cause is that the per-card Check updates action, added by the archived
`2026-10-26-agent-card-check-updates` change, gates the whole section through one view-model flag:

- The per-card Detect button's `IsEnabled` binds the **section-wide**
  `DataContext.AgentsState.CanDetect` (src/CLIHub/Views/SettingsAgentsPage.xaml:230), which turns
  false on every card the moment a per-card check starts.
- The per-card commands' predicates consult the section-wide availability
  (`_ => this.CanDetect` at src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:73 and
  `card.CanCheckUpdates && this.CanDetect` at :74/:226), which includes
  `!this.IsCheckingCardUpdates` (:188, :204) — so one running check disables both actions on every
  card.

The same section-wide flag also means the header actions are gated by a per-card check but not by a
per-card Detect, an asymmetry this change removes.

## What Changes

Decisions were confirmed by the user; this change implements exactly these:

1. **Per-card actions are independent across cards.** Starting per-card Detect or Check updates on
   card A disables only card A's own Detect and Check updates while it runs; every other card's
   Detect/Check stay enabled (subject to their own install/busy state).
2. **Per-card busy state moves onto `AgentCardItem`**: a new observable `IsDetecting` and a derived
   `CanDetect => !IsDetecting && !IsCheckingUpdates`; `CanCheckUpdates` becomes
   `IsInstalled && !IsDetecting && !IsCheckingUpdates`. Both busy setters and the `InstallState`
   setter raise the derived properties.
3. **`IsCheckingCardUpdates` is removed** from `IAgentManagementSection` and
   `AgentManagementViewModel`. Concurrency is tracked with a counter (`runningCardActionCount`)
   exposed as `IsCardActionRunning => runningCardActionCount > 0`, incremented/decremented by the
   per-card Detect and Check actions.
4. **Header-wide actions stay mutually exclusive with each other** and remain unavailable while ANY
   per-card action runs (`CanDetect`/`CanCheckUpdates` include `!IsCardActionRunning` — now also
   covering per-card Detect). Header passes continue to disable all per-card actions via the
   per-card command predicates.
5. **Per-card command predicates consult only the card and the header pass flags**: Detect =
   `card.CanDetect && !IsDetecting && !IsCheckingUpdates`; Check updates =
   `card.CanCheckUpdates && !IsDetecting && !IsCheckingUpdates`. They never consult
   `IsCardActionRunning` or other cards' state.
6. **`DetectCardAsync` sets/clears `card.IsDetecting` in try/finally** (in addition to the existing
   install-state handling); both per-card actions raise the per-card commands'
   `CanExecuteChanged` at start/end so WPF re-queries, and counter changes raise the header
   commands.
7. **XAML**: the per-card Detect `IsEnabled` changes from the section-wide
   `DataContext.AgentsState.CanDetect` to the card-local `{Binding CanDetect}`; the per-card Check
   binding stays `{Binding CanCheckUpdates}`; header bindings are unchanged.

A side effect worth noting: because the header actions are now gated by any per-card action
(including Detect), the parked hazard "Per-card Detect interleaving with a header action drops that
card's update indicator" (docs/backlog.md:188-200) is closed by this change — its proposed remedy
(a shared busy counter blocking the header check while any per-card detect runs) is exactly what
Decision 3 implements.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — MODIFIED requirement "Agent card actions": all nineteen scenario names preserved;
  bodies updated where the mutual-exclusion scope changes ("Detect available on every card" gains
  the card's-own-actions precondition; "Per-card Check updates is mutually exclusive" now describes
  same-card exclusion with other cards staying available); two new scenarios for per-card Detect and
  Check updates independence across cards.
- `agent-ui` — MODIFIED requirement "Agent management header actions": all nine scenario names
  preserved; the requirement body extends header gating to any per-card action (Detect or Check
  updates); "A per-card check gates the header actions" drops its now-false "per-card Detect is
  unavailable on every card" clause; one new scenario for a per-card Detect gating the header
  actions.
- `agent-update-detection` — **no delta**: the forced-check and host-installation-gate contracts
  are unchanged; only which actions may run concurrently changes.

## Impact

Source (all in `src/CLIHub`; `src/CLIHub.Core` untouched):

- `ViewModels/Agents/AgentCardItem.cs` — new observable `IsDetecting` and derived `CanDetect`;
  `CanCheckUpdates` gains `!IsDetecting`; `IsCheckingUpdates` and `InstallState` setters raise the
  newly derived properties; XML doc updates.
- `ViewModels/Agents/AgentManagementViewModel.cs` — `IsCheckingCardUpdates` removed;
  `runningCardActionCount`/`IsCardActionRunning` added; `CanDetect`/`CanCheckUpdates` swap the
  per-card-check flag for `!IsCardActionRunning`; per-card command predicates re-scoped to the card
  plus header flags; `DetectCardAsync` gains the card busy flag, counter enter/exit, and per-card
  command raises; `CheckCardUpdatesAsync` swaps its flag writes for counter enter/exit and gains
  the per-card command raises.
- `ViewModels/Agents/IAgentManagementSection.cs` — `IsCheckingCardUpdates` removed;
  `CanDetect`/`CanCheckUpdates` doc comments updated to "any per-card action". Only
  `AgentManagementViewModel` implements the interface (verified); test usages are
  `Mock.Of<IAgentManagementSection>()`, which absorb removals.
- `Views/SettingsAgentsPage.xaml` — one binding change (per-card Detect `IsEnabled` becomes
  card-local).

Tests (`tests/CLIHub.Tests`):

- `ViewModels/Agents/AgentManagementViewModelTests.cs` — the "every card disabled" test
  (`CheckCardUpdatesCommand_WhilePerCardCheckRuns_CanExecuteIsFalseOnEveryCardAndRestoresAfter`,
  :1453) is rewritten to assert only the acting card is disabled and other cards stay enabled; the
  `IsCheckingCardUpdates` assertions in :1645, :1687, :1787 move to `IsCardActionRunning`; the
  raise-count test (:1727) re-derives its counts and mid-check assertions; new tests for per-card
  Detect independence, a per-card Detect gating the header actions, and two concurrent per-card
  actions.
- `ViewModels/Agents/AgentCardItemTests.cs` — truth tables and notification coverage for
  `IsDetecting`, `CanDetect`, and the extended `CanCheckUpdates`.

Docs: `docs/backlog.md:188-200` — mark the per-card Detect/header interleaving hazard resolved by
this change. No image re-capture: the card action row is visually unchanged (only enablement
behavior changes).

## Non-goals

- No change to the header Detect / Check updates actions' own semantics, the `UpdateCheckSummary`
  text, or the load-time population passes.
- No change to the per-card Detect, Check updates, Update, or Edit config action *semantics* (what
  they do); only their availability/gating changes.
- No change to the per-card Update action's gating (declarative + running-agent blocked state) or
  to `CLIHub.Core` services.
- No per-card in-progress message or busy glyph beyond the existing "Detecting…" chip and disabled
  buttons — the disabled button remains the busy cue.
- No batching, queueing, or cancellation of concurrent per-card actions beyond the existing
  operation-lifetime behavior; two per-card actions on two different cards may genuinely run at
  once (that is the requested behavior).
- No fix for the unrelated parked item about a failed header check's ambiguous summary
  (docs/backlog.md:172-186).

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (working tree
  clean at `847c600`, baseline `openspec validate --all`: 29 passed, 0 failed).
- Builds on the archived `2026-10-26-agent-card-check-updates` baseline (the per-card Check updates
  action and the section-wide mutual exclusion this change re-scopes) and the
  `2026-10-26-agent-install-state` card model.
