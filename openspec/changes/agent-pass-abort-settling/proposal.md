# Make every aborted agent pass settle consistently

## Why

Three related defects let an aborted agent-management pass leave the CLI Agents page in a state the
user cannot trust, all parked in `docs/backlog.md` (lines 141-186) and all in
`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`:

1. **`DetectAsync`'s exception restore is partial.** It snapshots only `InstallState`
   (`:623`) and, on a thrown probe, restores only that field for every card (`:652-655`). A card
   that had already settled through `ApplyProbeResult` (`:785-798`) before the throw keeps the
   `Version`/`UpdateAvailability` the abandoned pass wrote for it, while its `InstallState` reverts
   to the pre-pass value. `DefaultAgentDetail` (`:126-143`) reads `Version` without looking at
   `InstallState`, so the default-agent summary can show a version next to an agent the selector
   (`AgentOption.DisplayName`, `AgentCardItem.cs:49-54`) marks "(Not installed)".
2. **`DetectAsync`'s disposed early return skips the restore entirely.** Every card is flipped to
   `Detecting` at pass start (`:627-630`). If `this.disposed` becomes true between probes, the loop
   returns (`:637-640`) without entering the `catch` that restores the snapshot, so every card the
   pass had not yet reached stays in `Detecting` forever (the `finally` only clears `IsDetecting`,
   never a card's own state).
3. **`CheckUpdatesAsync` has no exception handling at all.** It clears `UpdateCheckSummary` at the
   start (`:673`) and only rebuilds it after `Task.WhenAll` succeeds (`:700-706`). A thrown
   `CheckAsync` call propagates out of the method with the summary still `null` — which
   `BuildUpdateCheckSummary`'s own vocabulary (`:761-775`) never produces for a real "all checked,
   no updates" outcome, so a cleared summary is visually indistinguishable from "no updates found"
   rather than "the check failed."

All three are pre-existing, documented, low-severity gaps (exception path only; the application
self-heals on the next successful pass or reload). This change closes them in one place because
they share one root cause — a pass that announces "in progress" on every card up front but only
knows how to unwind a *subset* of that announcement — and one fix shape: track exactly which cards
a pass has already resolved, and settle the rest consistently however the pass ends.

## What Changes

- `DetectAsync`: on an aborted pass (thrown probe, or the disposed early return), restore the full
  pre-pass snapshot (`InstallState`, `Version`, `UpdateAvailability`) only for cards the pass had
  not yet applied a fresh `ApplyProbeResult` to. Cards the pass already settled keep their fresh,
  self-consistent result — a settled card is never rolled back once committed.
- `CheckUpdatesAsync`: catch an aborted pass and set `UpdateCheckSummary` to a short, explicit
  failure line (e.g. `"Update check failed"`) instead of leaving the cleared `null` to read as "no
  updates." Cards the pass already applied a fresh result to keep it; cards not yet reached keep
  their pre-pass advisory indicator (already true today — `InvalidateFor` precedes `CheckAsync` per
  card, so an unreached card's indicator is stale but present, never silently blanked). The header
  action becomes available again regardless of outcome (already guaranteed by the existing
  `finally`).
- Extract one shared "resolved-cards" bookkeeping shape (a `HashSet<AgentCardItem>` of cards a pass
  has committed a fresh result to) used by both `DetectAsync` and `CheckUpdatesAsync`'s abort paths,
  so the exception path and the `DetectAsync` disposed-early-return path share one settle routine
  instead of two hand-written loops.
- Extend `DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState` coverage with a card that
  settles before the throw (asserting it keeps its fresh result, not the restored snapshot) and add
  new tests for: `DetectAsync` disposed mid-pass leaving an unprobed card in `Detecting`, and
  `CheckUpdatesAsync` throwing mid-pass.

## Non-Goals

- No change to `IAgentProbeService`, `IAgentUpdateService`, or `IAgentUpdateAvailabilityService`
  contracts — this is caller-side bookkeeping only.
- No UI layout, styling, or theming change, and no new configuration key.
- No revisiting of the `CardActionScope` / `RestartablePass` design from the archived
  `agent-management-pass-consolidation` change; this change reuses that design as-is.
- No change to `DetectCardAsync` or `CheckCardUpdatesAsync` (the single-card actions): both already
  restore or discard correctly for a one-card scope (verified in `design.md`); only the header-wide,
  multi-card passes have the gap.
- No change to the load-time population passes (`PopulateVersionsAsync`, `PopulateBlockedStateAsync`)
  or their `RestartablePass` generation-guard behavior.

## Impact

- Affected capability specs: `agent-detection` (on-demand detection re-evaluation — the header
  Detect pass's settle guarantee) and `agent-update-detection` (on-demand update availability check
  — the header Check-updates pass's failure-summary guarantee). See `design.md` for why these two,
  not `agent-availability-display` or `agent-ui`.
- Affected source: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` only.
- Affected tests: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs`
  and `AgentManagementViewModelTests.UpdateAvailability.cs`.
- Closes three `docs/backlog.md` entries (lines 141-157, 159-170, 172-186) once implemented and
  verified; they should be marked done rather than deleted, per the file's own convention.

## Dependencies

- None on other active changes. `openspec/changes/` holds no other active change at the time of
  this proposal (working tree clean at `f963dba`).
- Builds on the archived `2026-10-30-agent-management-pass-consolidation` baseline (the
  `RestartablePass`/`CardActionScope` primitives this change leaves untouched) and the archived
  `2026-10-27-independent-card-actions` baseline (the per-card busy-state model this change does not
  touch).
