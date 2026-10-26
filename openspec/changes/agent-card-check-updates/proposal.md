# Per-agent "Check updates" action and Detect icon parity on the CLI Agents cards

## Why

Two user-reported problems on the per-agent cards of Settings → CLI Agents:

1. **The per-card Detect action renders the wrong glyph.** It uses `IconGlyphs.Diagnostic`
   (`\uE9D9`, src/CLIHub/Views/SettingsAgentsPage.xaml:240), which reads as a picture/photo glyph,
   while the header "Detect" button it mirrors uses `IconGlyphs.Refresh` (`\uE72C`,
   src/CLIHub/Views/SettingsAgentsPage.xaml:143). The user finds the per-card glyph confusing.
2. **No per-agent update check exists.** The header "Check updates" action
   (`AgentManagementViewModel.CheckUpdatesAsync`, src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:524-569)
   forces a fresh availability check for *every* installed agent at once. To answer "is there a
   newer version of *this* agent?" the user must hit every installed agent's declared distribution
   source. The per-card actions jump straight from Detect (local probe) to Update (runs the update
   command) with nothing between them.

## What Changes

Decisions were confirmed by the user; this change implements exactly these:

1. **Icon parity (XAML only).** The per-card Detect action renders `IconGlyphs.Refresh`, the same
   glyph as the header "Detect" button.
2. **New per-card "Check updates" action**, placed between Detect and Update (final order: Detect,
   Check updates, Update, Edit config). It runs a forced update-availability check for **that one
   agent only**: invalidate that agent's cached availability, then
   `IAgentUpdateAvailabilityService.CheckAsync`, and assign the fresh result to the card's
   `UpdateAvailability`. No host-installation detection and no version command at the call site.
   Glyph is `IconGlyphs.Sync`, the same as the header "Check updates" button
   (src/CLIHub/Views/SettingsAgentsPage.xaml:159).
3. **Naming.** Tooltip and `AutomationProperties.Name` are both `Check this agent for updates` —
   unique against the header action ("Check for agent updates") and the per-card Update ("Update
   agent" / "Run the agent's update command"), and stating its own work.
4. **Gating.** The action is unavailable when the card's `InstallState` is not `Installed`, while
   its own check runs, while any header-wide action runs, and while any other per-card check runs.
   Mutual exclusion mirrors the header contract in both directions: a header pass gates the per-card
   check on every card; a running per-card check gates both header actions and every per-card check.
   State model: a per-card busy flag (`AgentCardItem.IsCheckingUpdates` + derived
   `CanCheckUpdates`) plus one view-model flag (`IsCheckingCardUpdates`) feeding the existing
   `CanDetect`/`CanCheckUpdates` predicates, with `RaiseBusyStateChanges` extended so WPF re-queries
   `CanExecute` on every flip (exact model and rationale in design.md).
5. **Outcome feedback on the card's message line** (`AgentCardItem.Message`), exact wordings:
   `Update available · {installed} → {latest}` when an update is available, `Up to date` when up to
   date, `Update availability unknown` when the result is unknown/failed, and `Not installed on the
   host` on the defensive not-installed path. The header-wide `UpdateCheckSummary` is not touched by
   the per-card check and its wording is unchanged.
6. **Not-installed gate.** A not-installed card never requests its update source and clears any
   advisory indicator (defensive re-check inside the handler; the UI already refuses the action).
   Availability stays advisory: the Update action's availability never changes.
7. **Core untouched.** `IAgentUpdateAvailabilityService.InvalidateFor`/`CheckAsync` already provide
   per-agent invalidation and coalescing; the command is added to `IAgentManagementSection` and
   `AgentManagementViewModel`, the per-card properties to `AgentCardItem`, all in `src/CLIHub`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — MODIFIED requirement "Agent card actions": all thirteen original scenario names
  preserved ("Detect available on every card" gains the per-card-check precondition in its body);
  added scenarios for the per-card forced check (cache bypass with per-agent-only invalidation,
  not-installed gate, outcome feedback on the message line, busy/mutual exclusion), the
  disambiguating tooltip/automation name, and icon parity with the header actions.
- `agent-ui` — MODIFIED requirement "Agent management header actions": all eight original scenario
  names preserved; the mutual-exclusion contract extended both ways (header passes now also gate the
  per-card Check updates action; a running per-card check gates both header actions), one new
  scenario.
- `agent-update-detection` — **no delta**: the existing "On-demand update availability check" and
  "Update checks are gated on host installation" requirements already state the forced-check
  contract generically for any management-UI caller and any subset of checked agents; the per-card
  action is a new trigger of that contract, not a change to it (reasoning in design.md).

## Impact

Source (all in `src/CLIHub`; `src/CLIHub.Core` untouched):

- `ViewModels/Agents/AgentCardItem.cs` — new observable `IsCheckingUpdates` (per-card busy flag)
  and derived `CanCheckUpdates` (`IsInstalled && !IsCheckingUpdates`), with change notifications
  raised from the flag setter and from `InstallState` changes.
- `ViewModels/Agents/AgentManagementViewModel.cs` — new `IsCheckingCardUpdates` flag,
  `CheckCardUpdatesCommand` (`RelayCommand<AgentCardItem>`, mirrors `DetectCardCommand`),
  `CanCheckCardUpdates(card)` predicate, `CheckCardUpdatesAsync(card, ct)` handler;
  `CanDetect`/`CanCheckUpdates` gain `!IsCheckingCardUpdates`; `RaiseBusyStateChanges` raises the
  new command alongside the existing three.
- `ViewModels/Agents/IAgentManagementSection.cs` — new members `CheckCardUpdatesCommand` and
  `IsCheckingCardUpdates`; doc updates on `CanDetect`/`CanCheckUpdates` for the new gate.
  Only `AgentManagementViewModel` implements the interface (verified); test usages are
  `Mock.Of<IAgentManagementSection>()`, which absorb additions.
- `Views/SettingsAgentsPage.xaml` — per-card Detect glyph `Diagnostic` → `Refresh` (line 240); new
  Check updates button inserted between the Detect button (ends line 241) and the Update button
  (starts line 242).

Tests: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` and
`AgentCardItemTests.cs` — new coverage only; the change is additive to existing behavior
(`CanDetect`/`CanCheckUpdates` keep their old values whenever no per-card check runs), so existing
tests must stay green without modification.

Docs: `docs/images/settings-agents.png` re-captured during the smoke (the card action row changes
visually). No other doc describes the per-card actions.

## Non-goals

- No change to the header Detect / Check updates actions' own semantics, the `UpdateCheckSummary`
  text, or when the summary is written or cleared.
- No change to the load-time population passes (`PopulateVersions`, `PopulateBlockedState`).
- No change to the per-card Detect, Update, or Edit Config semantics (only the Detect glyph).
- No change to `CLIHub.Core` — the availability service already supports per-agent invalidation and
  in-flight coalescing; no `force` parameter is added to `CheckAsync`.
- No per-agent distinction between "no update source declared" and "check failed" in the outcome
  message — the service collapses both to `Unknown`.
- No batch/progress UI: no in-progress message on the card while its check runs; the disabled button
  is the busy cue.
- No fix for the pre-existing per-card-Detect/header-check interleaving hazard documented in
  `docs/backlog.md:188-200` (the new action does not worsen it; mutual exclusion actually closes the
  same shape for its own action).

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (working tree clean
  at `41400df`, baseline `openspec validate --all`: 29 passed, 0 failed).
- Builds on the archived `2026-10-26-agent-detect-update-actions` baseline (header actions, mutual
  exclusion, `RaiseBusyStateChanges` re-query pattern, forced-check contract) and the
  `2026-10-26-agent-install-state` card model, both now the durable spec baseline.
