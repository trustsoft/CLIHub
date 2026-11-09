# Settings → CLI Agents: installed agents first in the AGENTS card list

## Why

The AGENTS block on Settings → CLI Agents adds one card per catalog plugin in raw catalog order
(`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:486-531`) and binds that collection
directly (`src/CLIHub/Views/SettingsAgentsPage.xaml:203`). Catalog order interleaves agents that are
installed on the host with agents that are not, so the cards a user can actually act on are scattered
through a list whose only grouping is incidental.

The installation state is already known per card at load (`AgentManagementViewModel.cs:512`) and is
re-probed later by the header Detect pass (`:784-804`) and by each card's Detect action
(`:741-766`), but those transitions never move a card. A card that turns out to be absent after a
Detect therefore stays wherever catalog order put it.

The project already has a precedent for a pure, presentation-order policy applied by the caller:
`ProjectListOrdering.FavoritesFirst` (`src/CLIHub.Core/Projects/ProjectListOrdering.cs:10-41`),
applied at load in `ProjectPaneController.cs:86`. This change mirrors that shape for agent cards.

## What Changes

### New ordering policy

Add `src/CLIHub/ViewModels/Agents/AgentCardOrdering.cs` — a pure static policy with one method,
`InstalledFirst(IReadOnlyList<AgentCardItem> catalogOrder)`, that performs a stable partition:

- cards whose ordering group is installed come first;
- cards whose ordering group is not installed come last;
- the relative source (catalog) order is preserved inside each group; no alphabetical or other sort.

The ordering group is the card's last settled installation state: `AgentCardItem` gains an internal
ordering state that follows `InstallState` for `Installed`/`NotInstalled` and keeps the previous
settled value while `InstallState` is `Detecting`. That is what makes a header-wide Detect pass — which
sets every card to `Detecting` before probing — a no-op for ordering instead of a whole-list shuffle.
The policy lives in `CLIHub.ViewModels` because it consumes the UI type `AgentCardItem` and the
`AgentInstallState` enum. `CLIHub.Core` stays free of WPF/UI-card types. See design.md D1-D2.

### Live invariant in `AgentManagementViewModel`

- `Load` builds all cards in catalog order into a private catalog-ordered list, adds them to `Cards`
  in policy order, and keeps that list as the ordering source. `Cards` itself is never used as the
  ordering source, because after a move it no longer encodes catalog order.
- `Load` (and `Dispose`) detach the `PropertyChanged` handler from every previously observed card
  before the collection is rebuilt or the surface goes away.
- Each new card's `PropertyChanged` is observed. A change to `InstallState` re-applies the policy with
  a reconcile loop (`Move`, not `Clear`/re-add), so card identity, per-card state, and bound commands
  survive the reorder.
- A transient `Detecting` transition never re-applies the policy, and the card's ordering group stays
  at its last settled value while it is detecting.
- Settled `Installed`/`NotInstalled` changes do re-apply it, so a card that turns out to be absent
  moves into the not-installed end group and a card that turns out to be present moves into the
  installed group.

`RebuildDefaultAgentOptions` (`AgentManagementViewModel.cs:558-565`) builds the default-agent
dropdown from `Cards`, so the dropdown follows the new card order as a consequence. Existing
completion paths already call it after a pass settles; no extra notification is added by the reorder.

### Tests

- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardOrderingTests.cs` — empty list, all installed, all
  not installed, mixed list preserving relative order, and both last-settled `Detecting` cases
  (previously installed stays in front, previously not installed stays at the end).
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Ordering.cs` — load ordering for
  mixed/all-installed/all-not-installed catalogs, default-option order, Detect flipping a card to
  `NotInstalled` (moves into the not-installed group) and to `Installed` (moves up), the `Detecting`
  no-op, a one-move no-reentry check, and a header-pass check that a settle does not pull still
  detecting cards out of their last settled groups.
- Index-based assertions in existing agent-management tests are updated to select by `PluginId` only
  where the new order actually breaks them; no existing assertion is weakened.

### Spec delta

`openspec/specs/agent-ui/spec.md` gains one ADDED requirement, "AGENTS card list order", with
scenarios for mixed, all-installed, all-not-installed, both Detect flip directions, the `Detecting`
no-op, reload, and the untouched Disabled/Invalid sections. No existing requirement is modified.

### Documentation

`docs/project-context.md:66-67` gains one clause in the CLI Agents bullet: installed agents are listed
first, not-installed agents last, catalog order preserved within each group.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — ADDED requirement "AGENTS card list order". No existing requirement or scenario changes.

## Impact

- `src/CLIHub/ViewModels/Agents/AgentCardOrdering.cs` — new pure policy.
- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` — one internal ordering-state property and its
  maintenance in the `InstallState` setter; `InstallState` behavior and XAML bindings are unchanged.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — `Load`, `Dispose`, one new card-event
  handler, one catalog-ordered card list, and one reorder helper.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardOrderingTests.cs` — new tests.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Ordering.cs` — new tests.
- Existing agent-management tests — only index access that the new order breaks; selection moves to
  `PluginId`.
- `openspec/specs/agent-ui/spec.md` — one ADDED requirement.
- `docs/project-context.md` — one clause in the CLI Agents bullet.
- No XAML change: the AGENTS `ItemsControl` already observes the `ObservableCollection`.

## Non-goals

- No alphabetical, version, origin, or update-status sort. The only criterion is installation state.
- No change to the Disabled or Invalid sections, their order, or their contents
  (`SettingsAgentsPage.xaml:337`, `:400`).
- No change to the launch-window agent list or its composer order.
- No change to installation detection logic or probe results; the header Detect pass now visits cards
  in the displayed (installed-first) order. Version population, update availability, counts, and the
  default-agent selection semantics are unchanged. Only the option order follows the card order.
- No change to the public `InstallState` behavior or its XAML bindings; the new ordering state is
  derived and internal.
- No persistence of the display order; it is recomputed from current state on every load.
- No animation, delay, or grouping header in the AGENTS list.

## Dependencies

None. Before this change was created, `openspec list` reported no active changes, and no other open
change touches `AgentManagementViewModel`, `AgentCardItem`, or the `agent-ui` spec.
