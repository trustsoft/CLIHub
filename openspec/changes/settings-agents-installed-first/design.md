# Design: installed agents first in the AGENTS card list

## Context (verified current state)

All `file:line` references were verified against the working tree at
`9e49e6cad4979d15c159d09b0d94e0c15cf5c340` on 2026-11-09. `git status --porcelain` was empty before
this change directory was created, and `openspec list` reported no active changes before this change
was added.

### Card construction and the current order

`AgentManagementViewModel.Load(string? defaultAgentId)` is at
`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:474-542`:

- clears `Cards`, `InvalidCards`, and `DisabledCards` at `:482-484`;
- takes `catalog.GetAllPlugins()` at `:486` and adds one `InvalidAgentCardItem` per invalid entry at
  `:487-490` and one `DisabledAgentCardItem` per disabled entry at `:492-495`;
- loops the plugins in catalog order at `:510-531`, computes
  `detection.IsInstalledInSystem(plugin)` at `:512`, and calls `this.Cards.Add(card)` once per plugin
  at `:530`;
- refreshes derived surfaces at `:533-534`, including
  `RebuildDefaultAgentOptions(defaultAgentId)`.

No ordering step exists between construction and `Cards.Add`, so the bound collection is exactly
catalog order. The AGENTS block binds it directly:
`src/CLIHub/Views/SettingsAgentsPage.xaml:203` (`ItemsSource="{Binding AgentsState.Cards}"`). The
Disabled and Invalid sections bind separate collections at `:337` and `:400` and are not touched by
this change.

`Card.InstallState` is initialized from the constructor flag
(`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:83-93`, assignment at `:91`). The enum has three
members (`AgentCardItem.cs:12-28`): `Detecting`, `Installed`, `NotInstalled`. The setter at
`:135-148` raises `PropertyChanged` for `InstallState` (through `SetProperty`) and then for
`IsInstalled`, `VersionChipText`, `CanDetect`, and `CanCheckUpdates`. `IsInstalled` is derived at
`:154`.

### Detection flows mutate state after load

- Header `DetectAsync` (`AgentManagementViewModel.cs:768-833`) snapshots the cards at `:773`, sets
  **every** card to `Detecting` at `:784-787`, probes them one by one at `:791-804`, and settles each
  through `ApplyProbeResult` at `:802`. It rebuilds the summary and default options at `:806-807`.
  An abort restores unresolved cards through `SettleAbortedDetectPass` at `:809-827` and `:846-863`.
- Per-card `DetectCardAsync` (`:741-766`) remembers the previous state at `:744`, sets `Detecting` at
  `:745`, probes at `:748`, applies the result at `:749`, and rebuilds the summary and default options
  at `:756-757`. A thrown probe restores the previous state at `:759-764`.
- `ApplyProbeResult` (`:1001-1014`) writes `Installed` or `NotInstalled`, the version, and (for a
  missing agent) clears update availability.

Neither flow changes collection order. A card that settles as `NotInstalled` therefore keeps its
catalog position.

### Default-agent dropdown side effect

`RebuildDefaultAgentOptions` (`AgentManagementViewModel.cs:558-565`) builds the option list from
`this.Cards` in collection order at `:561`. Its call sites are `Load:534`, the version-population
completion `:671`, header Detect completion `:807` and its abort path `:823`, and per-card Detect
`:757`. Reordering `Cards` therefore also reorders the dropdown the next time options are rebuilt.
This is accepted and documented; no extra rebuild is added by the reorder itself.

### Existing presentation-order precedent

`src/CLIHub.Core/Projects/ProjectListOrdering.cs:10-41` is a pure static policy with no
dependencies. `FavoritesFirst` (`:18-40`) allocates two buckets, preserves source order in each, and
concatenates them. The caller applies it at load: `src/CLIHub/ViewModels/Projects/ProjectPaneController.cs:86`
(`var ordered = ProjectListOrdering.FavoritesFirst(this.projectService.GetAllProjects());`).
Tests live in `tests/CLIHub.Core.Tests/Projects/ProjectListOrderingTests.cs:6-96` and cover empty,
all-one-group, mixed, and stable-within-group cases.

The collection-reconcile pattern already exists for the launch window:
`src/CLIHub/ViewModels/Agents/AgentListSynchronizer.cs:61-72` walks the target list and calls
`rows.IndexOf` / `rows.Move` so row identity survives. The new reorder helper reuses that shape.

### Test project boundary

`tests/CLIHub.Core.Tests` references only `CLIHub.Core` (`docs/testing.md:19`), so it cannot see
`AgentCardItem` or `AgentInstallState`. A policy over cards must be tested in
`tests/CLIHub.Tests`, which references the WPF application (`docs/testing.md:21`).

The agent-management tests are split into partial files under
`tests/CLIHub.Tests/ViewModels/Agents/`; `AgentManagementViewModelTests.Load.cs:20-44` shows the
builder/assertion style, and `AgentManagementViewModelTests.Helpers.cs:13-29` defines the
`Launchable(id)` plugin helper. `AgentManagementViewModelBuilder`
(`tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs:286-293`) returns the configured
plugins from the default catalog mock in the order supplied.

Existing index-based access (`Cards[0]`, `Cards[1]`) is common, but every mixed-install-state test
selected during this investigation either keeps catalog order compatible with installed-first
(`Detection.cs:47-81`, `UpdateAvailability.cs:47-84`) or already selects by `PluginId`
(`Detection.cs:222-299`). The full suite must still be run; any test that breaks is updated to select
by `PluginId` rather than changing what it asserts.

## Decisions

### D1 — Policy location: `CLIHub.ViewModels`, not `CLIHub.Core`

**Decision:** add `AgentCardOrdering` under `src/CLIHub/ViewModels/Agents/`, typed over
`AgentCardItem`.

**Rationale:** the ordering criterion is the current installation knowledge carried by the card,
including the transient `Detecting` state, and the items being partitioned are the exact card objects
held by the bound `ObservableCollection<AgentCardItem>`. Keeping the policy over that type makes it
the single source of truth for both the load-time partition and the live re-apply. `CLIHub.Core`
cannot reference `AgentCardItem` without violating the project boundary
(`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:26`, `docs/project-context.md:19,35`),
and a Core policy over `(Plugin, bool isInstalled)` models only two states, so the `Detecting`
behavior would have to be implemented again in the view model.

**Rejected alternative — Core policy over `(plugin, isInstalled)` pairs:** allowed by the brief and
testable in `CLIHub.Core.Tests`, but it cannot express "a re-probing card keeps its previous group",
it forces a projection at each re-apply, and the live trigger still lives in the view model. It adds
indirection without moving any decision out of the UI layer. The pure-function property that matters
— no dependencies, no mutation of inputs, deterministic — is preserved either way.

**Rejected alternative — sort in XAML with `CollectionViewSource`/`SortDescription`:** cannot express
"current state group, otherwise catalog order" without a synthetic sort key, would react to the
`Detecting` transition, and would leave the underlying collection in a different order than the
dropdown built from it.

### D2 — Policy semantics: a card's ordering group is its last settled state

**Decision:** add an internal ordering state to `AgentCardItem`:

- `OrderingState` returns `InstallState` when it is `Installed` or `NotInstalled`;
- while `InstallState` is `Detecting`, `OrderingState` returns the last settled state;
- the setter updates the remembered state only when the new value is `Installed` or `NotInstalled`;
  the constructor initializes it from the initial flag.

`AgentCardOrdering.InstalledFirst` appends a card to the end group only when
`card.OrderingState == AgentInstallState.NotInstalled`; every other card goes to the front group.

The `InstallState` setter raises `PropertyChanged` from `SetProperty` before it can assign the
remembered state, so the reorder handler may run while `lastSettledState` is still the previous
value. This is safe because `OrderingState` reads the live `installState` for `Installed` and
`NotInstalled`; only a `Detecting` value consults `lastSettledState`, and the handler skips that
transition. The setter then records the settled value for the next `Detecting` episode.

**Rationale:** "`Detecting` as do not move" means a card being re-probed must keep the group it had
before the probe started. Reading the live `InstallState` would make every card in a header pass
temporarily `Detecting`; any classification of that value either treats all cards as installed (and
regroups the previously missing cards into the front) or treats them as missing (and pushes the
previously installed cards to the end). Both shuffle the list mid-pass and destroy catalog order.
Freezing the group at the last settled state makes the pass announcement a no-op for ordering, while
a settled `Installed`/`NotInstalled` change moves exactly the card that changed.

**Rejected alternative — treat `Detecting` as installed/front:** on a load order of
`[A (installed), C (installed), B (missing), D (missing)]`, the first settled card would regroup the
still-detecting `B`/`D` into the front and reorder the list mid-pass.

**Rejected alternative — pin `Detecting` cards at their current index:** requires an index-aware
partition plus a second reconciliation rule, and the frozen-group rule already keeps every
`Detecting` card in place without special-case index logic.

### D3 — The ordering source is a catalog-ordered card list, not `Cards`

**Decision:** `Load` keeps a private `catalogOrderedCards` list containing every card in the order
`GetAllPlugins()` returned. `Load` and every re-apply compute the target order from that list, never
from the current `Cards` order.

**Rationale:** a stable partition preserves the order of its *input*. Once a card has moved into or
out of a group, `Cards` no longer encodes catalog order, so partitioning `Cards` can reverse two
cards inside a group. Example: catalog `[A, B, C]` with loaded order `[A, C, B]`; if `C` becomes
not installed, partitioning `Cards` yields `[A, C, B]` for the missing group `[C, B]`, but catalog
order is `[B, C]`. Reading the untouched `catalogOrderedCards` list always yields the correct group
order. The list's membership is exactly `Cards`' membership by construction: both are rebuilt only in
`Load`, and no other code adds or removes cards.

**Rejected alternative — add a catalog index property to each card and sort by it:** equivalent
outcome, but it sorts instead of partitioning and spreads the catalog-order fact across every card.
Keeping the one catalog-ordered list makes the policy stay a plain stable partition with the source
order as its only secondary key.

### D4 — Live trigger: card `PropertyChanged`, settled states only

**Decision:** the view model subscribes to `PropertyChanged` on every card it adds. The handler:

1. ignores everything except `nameof(AgentCardItem.InstallState)`;
2. ignores `Detecting` (a no-op for ordering, per D2);
3. ignores a disposed view model;
4. calls `ApplyCardOrdering()`.

**Rationale:** the brief requires the list to re-order when a Detect settles a card, including the
per-card Detect path, which has no collection-level signal. Card property changes are the only
notification that fires at the exact moment the state settles. Filtering on the property name avoids
work for version, message, availability, and busy-flag changes, which are frequent during a pass.
Because the target order comes from `catalogOrderedCards` and the changed card's `OrderingState`,
a settle moves only the cards whose group membership actually changed.

**Rejected alternative — call the reorder only at the end of `DetectAsync` / `DetectCardAsync`:**
would miss the settled moments inside a header pass, is called twice (normal and abort paths), and
does not cover future writers of `InstallState`; the property-driven trigger covers all of them.

### D5 — No re-sort loop

**Decision:** the only subscription is per-card `PropertyChanged`; the reorder mutates
`ObservableCollection<AgentCardItem>` with `Move`.

**Rationale:** `ObservableCollection.Move` raises `CollectionChanged`, not `PropertyChanged` on any
card, so the handler cannot re-enter itself. The handler never writes `InstallState`. No
`CollectionChanged` subscription is added. The `disposed` guard covers a late probe continuation. A
test asserts that one settled change produces exactly one `Move` notification and a stable final
order.

**Rejected alternative — subscribe to `Cards.CollectionChanged` to keep the invariant:** would
re-enter on every `Move` and would need a re-entrancy flag; it also cannot distinguish a card-state
change from a `Load`-driven rebuild. Not needed.

### D6 — Subscription lifecycle

**Decision:** `Load` iterates the current `Cards` and detaches the handler before `Cards.Clear()`;
`Dispose` does the same before setting `disposed = true` and clears `catalogOrderedCards`; new cards
are attached after the ordered adds.

**Rationale:** the set of observed cards is exactly the set currently in `Cards` by construction, so
no separate observed-set needs to be kept consistent. Detaching in `Load` is required because `Load`
is the `PluginsChanged` handler (`AgentManagementViewModel.cs:553-556`) and can run many times;
without it old cards would stay rooted by the view model and a late probe continuation could trigger
a re-apply against the new collection. Detaching in `Dispose` prevents the same leak on surface
teardown.

**Rejected alternative — keep a `HashSet<AgentCardItem>` of observed cards:** duplicates the
membership fact already held by `Cards` and adds a second thing to keep in sync. The implementation
order (build, order, add, attach) rules out a card attached but not in `Cards`.

### D7 — Reconcile with `Move`, not rebuild

**Decision:** `ApplyCardOrdering` computes `AgentCardOrdering.InstalledFirst(this.catalogOrderedCards)`,
then walks target indexes over `Cards` and calls `Cards.Move(currentIndex, targetIndex)` for each
displaced card, following `AgentListSynchronizer.cs:61-72`.

**Rationale:** `Clear` + re-add would recreate visual containers, discard focus and scroll position,
and disturb per-card transient state held in the same objects for no reason. `Move` keeps card
identity and only changes position. The list is small (tens of agents), so `IndexOf`/`Move` per
position is not a performance concern.

**Rejected alternative — fully diff by identity into a new `ObservableCollection`:** replacing the
collection instance would break the XAML binding, which observes the existing instance. Rebuilding
contents with `Clear` has the churn described above.

### D8 — Reorder does not rebuild derived surfaces

**Decision:** the `PropertyChanged` handler only reorders `Cards`. It does not call
`RefreshAvailabilitySummary` or `RebuildDefaultAgentOptions`.

**Rationale:** every existing writer of `InstallState` already rebuilds those surfaces on its
completion path (`DetectAsync:806-807`, its abort path `:823`, `DetectCardAsync:756-757`). Adding a
rebuild in the handler would duplicate those notifications during a pass. The observable side effect
remains: whenever options are next rebuilt, they follow the card order from
`RebuildDefaultAgentOptions:561`.

### D9 — Spec delta is ADDED, not MODIFIED

**Decision:** add one requirement to the `agent-ui` capability through an `## ADDED Requirements`
delta in this change; do not touch existing requirements.

**Rationale:** the `agent-ui` spec has no requirement about AGENTS card order (verified by searching
the capability file). There is therefore no original requirement or scenario name to preserve.
`skip_specs: true` would be wrong: the order is durable, user-visible behavior that the existing
suite would not otherwise guard.

### D10 — Tests split by what each project can see

**Decision:** policy tests and view-model tests both live in `tests/CLIHub.Tests`; no Core test is
added.

**Rationale:** `CLIHub.Core.Tests` references only Core and cannot compile a test that mentions
`AgentCardItem` (`docs/testing.md:19`). Putting the policy in ViewModels forfeits the Core test
project by design (D1); the same behaviors are covered in `CLIHub.Tests` with the same
`MethodOrScenario_Condition_ExpectedResult` naming.

## Deviations from the brief

None. The brief offered a Core `(plugin, isInstalled)` policy or a ViewModels `AgentCardItem` policy;
D1 chooses the latter with a stated reason. The brief's "Detecting as do not move" is implemented by
freezing the ordering group at the last settled state (D2) and by using the catalog-ordered source
(D3), so a `Detecting` transition itself never reorders anything and the pass cannot shuffle the
list. `AgentCardItem` gains one internal ordering property; that is the mechanism for D2, not a
change to the public `InstallState` behavior.

## Open questions

1. **Default-agent dropdown order.** The dropdown will follow the card order whenever it is rebuilt
   (installed first, then not installed), because it is projected from `Cards`. This is a visible
   side effect the brief asked to record, not to change. If the user prefers the dropdown to stay in
   catalog order, the implementation needs a separate catalog-ordered source for options; the current
   plan accepts the follow-the-cards behavior.
2. **Accessibility announcement.** Reordering an `ItemsControl` while the user is on the page is not
   announced by Narrator. No automation live-region or focus restoration is planned; confirm this is
   acceptable for a settings surface.
3. **Ordering state visibility.** `AgentCardItem.OrderingState` is planned as internal (the policy is
   in the same assembly, no XAML binding, no test reads it directly). If the implementation session
   finds a need to assert it from `CLIHub.Tests`, it must either add a public read-only property or an
   `InternalsVisibleTo` entry; prefer asserting the public policy output instead.

## Implementation notes (recorded during implementation)

1. **One existing test arrangement changed because the header pass now probes in card order.**
   `DetectCommand_WhenProbeThrowsAfterACardSettled_KeepsThatCardAndRestoresTheRest`
   (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs`) previously
   loaded `a` as not installed and `b`/`c` as installed. With catalog `[a, b, c]` that made the old
   catalog-order collection probe `a` first and let `a` settle before `b` threw. Installed-first
   ordering puts `a` last, so `b` (an installed card) is probed first and the pass throws before `a`
   is reached. The test's invariant is unchanged — a committed fresh probe result survives the abort
   rollback and the default-agent summary agrees with its card — so all three cards now load installed
   and `a` flips to `NotInstalled` in the pass while `b` throws and `c` is never probed. No assertion
   was relaxed; the assertions were retargeted to the committed `NotInstalled` result
   (`Version` null, `In the launch window`, `"a (Not installed)"`). `openspec/specs/agent-ui/spec.md`
   is untouched; the delta already describes this behavior.
2. **One task example's expected order contradicted D3.** Task 3.3's
   `DetectCommand_WhenPassSettlesMixedStates_OrdersInstalledFirst` expected `[c, b, a]` for catalog
   `[a, b, c]` with `b` settling installed. Catalog order within the installed group is `[b, c]`, so
   the correct final order per D3 and the delta is `[b, c, a]`; the test asserts that order. The
   spec delta is unchanged.
3. **No other deviation.** The delta in
   `openspec/changes/settings-agents-installed-first/specs/agent-ui/spec.md` matches the implemented
   behavior, and `src/CLIHub/CLIHub.csproj` already has `<InternalsVisibleTo Include="CLIHub.Tests" />`,
   so `OrderingState` stayed internal and no test reads it directly.
