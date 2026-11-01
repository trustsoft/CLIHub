## 1. `DetectAsync` settle-consistency fix

- [x] 1.1 Read `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` around `DetectAsync`
      (`:617-664` at HEAD `f963dba`; re-verify line numbers first) and `ApplyProbeResult`
      (`:785-798`). Confirm the two bugs described in `design.md` (partial-field restore; disposed
      early return skips the catch) still reproduce before changing anything.
- [x] 1.2 Widen the pre-pass snapshot from `Dictionary<AgentCardItem, AgentInstallState>` to a
      per-card `(AgentInstallState InstallState, string? Version, AgentUpdateAvailability?
      UpdateAvailability)` snapshot (named tuple or small private record — follow the file's
      existing style for similar shapes).
- [x] 1.3 Add a `HashSet<AgentCardItem> resolved` (or equivalent) populated immediately after each
      successful `ApplyProbeResult` call inside the per-card probe loop.
- [x] 1.4 Extract a private settle helper that, given `cards`, `resolved`, and the snapshot,
      restores the full pre-pass triple only for cards not in `resolved`, leaving resolved cards
      untouched.
- [x] 1.5 Call the helper from the existing `catch (Exception)` block (replacing the current
      `InstallState`-only loop) before `throw;`.
- [x] 1.6 Replace the bare `if (this.disposed) { return; }` early return with a call to the same
      helper before returning, so a disposed-mid-pass abort settles identically to a thrown-probe
      abort.
- [x] 1.7 Build: `dotnet build CLIHub.sln`. Fix any compile errors before proceeding.

## 2. `CheckUpdatesAsync` failure-summary fix

- [x] 2.1 Read `CheckUpdatesAsync` (`:666-712` at HEAD `f963dba`; re-verify first) and
      `BuildUpdateCheckSummary` (`:761-775`). Confirm there is still no `catch` around the
      `Task.WhenAll` block.
- [x] 2.2 Wrap the `Task.WhenAll` call and the subsequent summary-build/refresh block in
      `try`/`catch (Exception)`. On catch, when `!this.disposed`, set `UpdateCheckSummary` to a
      short explicit failure line (e.g. `"Update check failed"` — see `design.md` Open Questions
      for exact wording latitude) and call `RefreshAvailabilitySummary()`, then rethrow.
- [x] 2.3 Verify the rethrow still reaches the existing `RunAsync`/`ApplicationOperationLifetime`
      logging path unchanged (no new swallow introduced) — read the call site at `:81` and the
      lifetime's existing failure-logging behavior to confirm, do not just assume.
- [x] 2.4 Build: `dotnet build CLIHub.sln`. Fix any compile errors before proceeding.

## 3. Tests

- [x] 3.1 Extend `DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState`
      (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs:205-231`) or
      add a sibling test: with three cards where the first probe succeeds (and
      `ApplyProbeResult` commits a fresh `Version`/`InstallState`) before the second probe throws,
      assert the first card **keeps** its fresh settled result (not the pre-pass snapshot) while
      the un-probed third card is fully restored to its pre-pass `InstallState`, `Version`, and
      `UpdateAvailability`. Do not weaken the existing assertions in that test.
- [x] 3.2 Add a new test for the disposed-mid-pass path: start `DetectCommand`, gate the second
      card's probe open, call `viewModel.Dispose()` while the first card's probe is still pending
      (or between cards), let the gated probe resolve, and assert no card is left with
      `InstallState == AgentInstallState.Detecting`.
- [x] 3.3 Add a new test for `CheckUpdatesCommand` throwing mid-pass: configure
      `IAgentUpdateAvailabilityService.CheckAsync` to throw for one card, execute
      `CheckUpdatesCommand`, and assert `UpdateCheckSummary` is the explicit failure text (not
      `null` and not a `BuildUpdateCheckSummary`-shaped string), `CanCheckUpdates`/`CanDetect`
      become true again afterward, and a card whose check completed before the throw keeps its
      fresh `UpdateAvailability`.
- [x] 3.4 Run `dotnet test CLIHub.sln --filter "FullyQualifiedName~AgentManagementViewModelTests"`
      and verify every test in that namespace passes, including the pre-existing
      `DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState`,
      `CheckCardUpdatesCommand_WhenCheckThrows_RestoresGatesAfterFailure`, and
      `DetectCardCommand_WhenProbeThrows_RestoresPreviousCardState` scenarios unchanged in
      assertion strength.

## 4. Spec deltas

- [x] 4.1 Create `specs/agent-detection/spec.md` under this change directory with a `## MODIFIED
      Requirements` section widening "On-demand detection re-evaluation": keep every existing
      scenario name verbatim (`Manual probe bypasses the cache`, `Probe reports installed`, `Probe
      reports not installed`, `Probe requires no project`) and add new scenarios covering: a probe
      that throws mid-pass settles already-resolved cards with their fresh result and unresolved
      cards back to their pre-pass state; a pass abandoned by disposal settles every unresolved
      card out of `Detecting`.
- [x] 4.2 Create `specs/agent-update-detection/spec.md` under this change directory with a `##
      MODIFIED Requirements` section widening "On-demand update availability check": keep every
      existing scenario name verbatim (`Forced check bypasses the cache`, `Forced check invalidates
      only checked agents`, `Forced check performs no detection or version command at the call
      site`, `Forced check gates on host installation`, `Forced check assigns the fresh result`,
      `Availability service behavior is unchanged`) and add one new scenario: a forced check that
      throws mid-pass reports an explicit failure summary rather than leaving the summary empty,
      while any card that already received a fresh result before the failure keeps it.
- [x] 4.3 Run `openspec validate --all` and confirm it passes with no errors; quote the exact
      pass/fail totals in the final report.

## 5. Final verification and backlog note (implementation session)

- [x] 5.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` for the full solution (not just
      the filtered subset) and confirm a clean, all-green result.
- [x] 5.2 Update `docs/backlog.md` entries at lines 141-157 ("Detect failure restores install state
      but not the version"), 159-170 ("Dispose racing a Detect pass..."), and 172-186 ("A failed
      manual update check leaves no summary..."): mark each `[done <date>: agent-pass-abort-settling]`
      with a one-line pointer to this change's commit, per the file's existing convention for closed
      items. Do not delete the entries — keep the evidence trail.
- [x] 5.3 Run `node .opencode/scripts/check-model-policy.mjs` if subagents were dispatched during
      implementation; otherwise skip.
- [x] 5.4 Run `graphify update .` to refresh the knowledge graph after the source edits.
- [x] 5.5 Report the final gate results (build, test, validate) to the user and wait for explicit
      approval before archiving this change or committing, per `AGENTS.md`'s change/commit/archive
      policy.
