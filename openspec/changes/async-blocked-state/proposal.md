# Populate the agent blocked state asynchronously on the agents page

## Why

`AgentManagementViewModel.Load` still runs the running-agent enumeration synchronously on the
UI thread (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:195-198`), and
`SettingsWindow.ShowSettings` loads before it shows
(`src/CLIHub/Views/SettingsWindow.xaml.cs:40-54`), so that cost lands directly on Settings open —
the window cannot appear until `Load` returns. After `fix-settings-open-delay` the pass is cheap
(one `Process.GetProcesses` snapshot plus the candidate-name pre-filter,
`src/CLIHub.Core/Agents/AgentProcessMonitor.cs:72-73`) but not free, and any future cost added to
that path lands straight back on the open path.

This is the parked backlog item "Async blocked-state population on the agents page"
(`docs/backlog.md:20-35`, severity Medium) — design D2 of
`openspec/changes/archive/2026-10-20-fix-settings-open-delay/design.md:71-97`, rejected there only
because D3 reached the latency goal with no observable change. Two things make it the right time:

- `consolidate-version-population` (2026-10-24) landed the canonical fire-and-forget population
  shape in this exact class — `versionPopulationCts`/`versionPopulationGeneration` plus
  `PopulateVersionsAsync(cards, generation, refreshToken, applicationToken)` with linked tokens,
  generation-guarded writes, local catches, and cancel-then-flag `Dispose`
  (`AgentManagementViewModel.cs:33-34`, `:265-322`, `:230-237`). The blocked-state pass is now a
  mechanical application of a proven, spec-recorded pattern, not a new design.
- The blocker recorded in the backlog — the state-based `agent-ui` scenario "Update blocked by a
  running agent" (`openspec/specs/agent-ui/spec.md:233-236`) — is a spec matter that this change
  settles explicitly instead of leaving the transient unblessed.

## What Changes

**Behavior:**

- Cards are built with `CanUpdate = declaresUpdate` (declarative availability, matching the launch
  pane's `AgentItem.CanUpdate`, `src/CLIHub/ViewModels/Agents/AgentItem.cs:79`) and no blocked
  message; the CLI Agents page renders immediately on Settings open.
- A fire-and-forget blocked-state pass starts at `Load` (only when at least one plugin declares an
  `update` command), enumerates running agents **off the UI thread**, and writes each card's
  `CanUpdate` and blocked-state `Message` when the enumeration completes.
- Blessed transient: until the pass lands, a card whose agent is running may show "Update" as
  available. Choosing "Update" in that state still does not start the update — the execution-time
  gate in `AgentManagementService.UpdateAsync`
  (`src/CLIHub.Core/Agents/AgentManagementService.cs:76-83`) blocks it and the card reports the
  block, exactly as it already does for a stale snapshot today.
- Re-`Load` supersedes an in-flight pass; closing the Settings window cancels it. An enumeration
  already started may complete, but its result is not applied to the abandoned surface.
- Enumeration failure behaves as today: the monitor logs and returns an empty list, so the cards
  settle at declarative availability (fail-open); an unexpected pass failure is logged and leaves
  the cards unchanged.

**Code:**

- `AgentManagementViewModel` gains `blockedStateCts`/`blockedStateGeneration` fields, a
  `PopulateBlockedState`/`PopulateBlockedStateAsync` pair in the canonical shape (sibling of the
  version pass, own CTS/generation, operation name "Agent blocked-state population"), a `Load`
  rewrite that drops the synchronous enumeration, and a `Dispose` that cancels both passes.
- No XAML change (the Update button already binds `IsEnabled="{Binding CanUpdate}"`,
  `src/CLIHub/Views/SettingsAgentsPage.xaml:201`), no `CLIHub.Core` change, no monitor change, no
  DI change.
- Spec delta: `agent-ui`'s "Agent card action availability" requirement is modified with all five
  original scenario names preserved verbatim, plus five new scenarios (async population, the
  blessed transient, reload supersession, surface-exit cancellation, failure logging).

## Impact

**User-visible:**

- Settings opens without the running-agent enumeration on the UI thread; cards render instantly
  with the declarative Update state and the blocked state (when any) lands moments later (the
  pre-filtered enumeration is typically tens of milliseconds).
- For that brief window a blocked card can show "Update" as available; clicking it is still
  blocked at execution time with the standard "Update blocked while the agent is running: …"
  message.

**Internal:**

- One production file (`AgentManagementViewModel.cs`) and its test file; `SettingsWindow`,
  `SettingsViewModel`, `AgentProcessMonitor`, `WindowsAgentProcessInspector`,
  `AgentManagementService`, `WindowActionCoordinator`, and `AgentPaneController` are untouched.
- `SettingsWindow.ShowSettings` keeps its load-before-show order — the backlog listed a
  restructure as a consequence, but the fire-and-forget pass removes the load-blocking cost, so
  the restructure is unnecessary (design D5 challenges it; the resolved backlog entry corrects
  the record).
- No CHANGELOG entry now: the two most recent changes (`consolidate-version-population`,
  `hygiene-sweep`) also deferred their entries to the next release heading; this one follows that
  convention.

**Tests:**

- Four new `AgentManagementViewModelTests` (declarative interim + settle, dispose cancellation,
  reload supersession, enumeration failure) beside the existing population tests; the five
  existing blocked-state tests and the two enumeration-count tests stay green **unchanged** — the
  default `InlineOperationLifetime` test seam runs the pass synchronously to completion inside
  `Load` (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:571-583`).

## Non-goals

- Restructuring `SettingsWindow.ShowSettings` (show-before-load or async load) — the window keeps
  opening with every page populated; see design D5.
- The "Monitor-level running-agent cache" backlog item (`docs/backlog.md:37-48`) — the monitor
  stays cacheless; caching without an invalidation story would stale the execution-time gate.
- The "Runtime-hosted agent blocking (Option B)" backlog item (`docs/backlog.md:50-60`) — no
  command-line retrieval; matching stays executable-name-based.
- Any change to the launch window's Update gating (`WindowActionCoordinator.CanExecuteUpdate` /
  `ExecuteUpdate`, `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-182`) — it stays
  synchronous per requery.
- Any change to the execution-time safety gate in `AgentManagementService.UpdateAsync`
  (`AgentManagementService.cs:76-83`) — it stays synchronous and exact.
- New UI states or visuals for the Update action (busy/placeholder visuals, "Checking…" text) —
  the declarative value *is* the interim treatment; a visible treatment is a separate UI change
  if the transient ever proves confusing.
- Refreshing the blocked state on `Test`/`Detect`/`Update` actions or on process-start events —
  `Load` remains the refresh trigger, as today (`AgentManagementViewModel.cs:190-194`).
- Touching the version-population loop, the `agent-version` spec, or the legacy
  `Views/MainWindow.xaml.cs`.

## Dependencies

None — no other active OpenSpec change exists. The change builds on two archived ones without
reopening them: `consolidate-version-population` (2026-10-24) supplies the canonical population
shape this change replicates, and `fix-settings-open-delay` (2026-10-20) supplies the single
enumeration + candidate pre-filter semantics the pass preserves (its D1 grouping moves inside the
pass unchanged) and the D2 design this item tracks.
