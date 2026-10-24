# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive (and per
the repository policy, both wait for the manual smoke in 2.8).

## Stage 1: The async blocked-state pass (`AgentManagementViewModel` + tests)

**Goal**: `Load` builds cards with declarative Update availability and starts a cancellable,
off-the-UI-thread blocked-state pass in the canonical population shape; settled behavior is
unchanged.

- [ ] 1.1 `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`: add fields
  `private CancellationTokenSource? blockedStateCts;` and `private int blockedStateGeneration;`
  beside `versionPopulationCts`/`versionPopulationGeneration` (lines 33-34).
- [ ] 1.2 Same file, rewrite `Load` (lines 179-227): remove the synchronous enumeration and
  `runningAgentsByPlugin` lookup (lines 195-198) and the per-card `runningAgents`/`blockedNames`
  computation (lines 205-208); build each card with `CanUpdate = declaresUpdate` and **no**
  `Message` initializer. Keep the `anyDeclaresUpdate` computation (it now guards starting the
  pass). Replace the intent comment (lines 190-194) with one stating: cards render with
  declarative availability; the running-agent blocked state populates through a fire-and-forget
  pass (Load stays the refresh trigger); the execution-time block stays in
  `AgentManagementService`; one enumeration feeds every card (grouping the single result by
  plugin id preserves the per-plugin partitions, first-match attribution).
- [ ] 1.3 Same file, add `private void PopulateBlockedState()` mirroring `PopulateVersions`
  (lines 265-278) exactly: cancel-and-dispose `blockedStateCts`, create a new CTS,
  `var generation = ++this.blockedStateGeneration;`, capture the token, snapshot
  `this.Cards.ToList()`, and start
  `_ = this.RunAsync("Agent blocked-state population", applicationToken =>
  this.PopulateBlockedStateAsync(cards, generation, refreshCancellationToken, applicationToken))`.
  Call it from `Load` **before** `this.PopulateVersions()` (line 226) so the safety-relevant
  state arrives first, guarded by `anyDeclaresUpdate`.
- [ ] 1.4 Same file, add
  `private async Task PopulateBlockedStateAsync(IReadOnlyList<AgentCardItem> cards, int
  generation, CancellationToken refreshCancellationToken, CancellationToken applicationToken)`
  mirroring `PopulateVersionsAsync` (lines 280-322): link the two tokens; run the enumeration off
  the UI thread — `var runningAgents = await Task.Run(() =>
  this.processMonitor.GetRunningAgents(), cancellationToken);` (design D4 — without `Task.Run`
  the pass executes on the UI thread and the change is a latency no-op); group with
  `runningAgents.ToLookup(agent => agent.Plugin.Id)`; then under
  `!cancellationToken.IsCancellationRequested && generation == this.blockedStateGeneration`,
  per card write `card.CanUpdate = declaresUpdate && running.Count == 0` and — **only when
  blocked** — `card.Message = $"Update blocked while the agent is running: {blockedNames}"`
  (design D3: never clear a `Message`; keep the current format string from line 218 byte-identical).
  Do not touch `RefreshAvailabilitySummary`/`RebuildDefaultAgentOptions` (neither reads
  `CanUpdate`/`Message`). Wrap the body in the two canonical local catches: silent
  `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` and
  `catch (Exception ex) { this.logger.LogError(ex, "Agent blocked-state population failed"); }`.
- [ ] 1.5 Same file, extend `Dispose` (lines 230-237): before `disposed = true`, also
  `this.blockedStateGeneration++; this.blockedStateCts?.Cancel();
  this.blockedStateCts?.Dispose(); this.blockedStateCts = null;` — mirroring the
  version-population block already there. Keep the `disposed` flag and the `Load`/`DetectAsync`
  guards unchanged.
- [ ] 1.6 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`: verify — do
  **not** edit — that the seven existing pins stay green unchanged:
  `Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock` (lines 418-436),
  `Load_OtherAgentsProcessRunning_KeepsUpdateAvailableWithoutMessage` (438-458),
  `Load_WithNoRunningProcesses_KeepsDeclaredUpdateAvailableWithoutMessage` (460-472),
  `Load_MultipleUpdateDeclaringPlugins_EnumeratesProcessesOnce` (474-493),
  `Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses` (495-513),
  `Load_BuildsCardsCountsAndDefaultOptions` (97-118), and
  `Load_UnknownAvailability_LeavesCanUpdateUnchanged` (298-313). The default
  `InlineOperationLifetime` (lines 571-583) runs the new pass synchronously to completion inside
  `Load`, so their settled-state assertions pin the unchanged outcome through the new shape. If
  any fails, the implementation deviated from the settled behavior — fix the code, not the tests.
- [ ] 1.7 Same test file, add
  `Load_ShowsDeclarativeAvailability_BeforeBlockedStateLands`: one update-declaring plugin;
  `DeferredOperationLifetime` (lines 585-593); inspector mock whose `GetRunningProcesses` blocks
  on a `ManualResetEventSlim` and then returns a matching `AgentProcessInstance` (pattern of
  `Processes(...)`, lines 75-80). After `Load(null)` assert `card.CanUpdate` is true and
  `card.Message` is null (the blessed transient); release the gate; wait for the settle with a
  bounded spin (for example `SpinWait.SpinUntil(() => !card.CanUpdate, TimeSpan.FromSeconds(5))`);
  assert the card settles at `CanUpdate == false` with the blocked message.
- [ ] 1.8 Same test file, add `Dispose_CancelsBlockedStatePass`: same gated-inspector setup;
  `Load(null)`; `viewModel.Dispose()`; release the gate; assert `card.CanUpdate` stays true and
  `card.Message` stays null (the token + generation guard discards the write). The assertion
  pattern — assert a value the guard makes unreachable — is the existing benign-race shape of
  `Dispose_CancelsInFlightPopulation`/`Load_SecondLoadCancelsFirstPass` (lines 177-243).
- [ ] 1.9 Same test file, add `Load_SecondLoadCancelsBlockedStatePass`: inspector mock whose
  first call blocks on a gate and (once released) returns a matching process, whose second call
  returns none immediately; `DeferredOperationLifetime`; capture the first `Load`'s card object;
  `Load(null)` twice; release the gate; assert the **new** cards settle unblocked (`CanUpdate`
  true, no message) and the captured **old** card still has `CanUpdate == true` — the first
  pass's would-be blocking write was rejected by the generation guard (its snapshot is also
  detached, but the guard is the tested mechanism).
- [ ] 1.10 Same test file, add `Load_WhenEnumerationFails_KeepsDeclaredAvailability`: inspector
  mock whose `GetRunningProcesses` throws `InvalidOperationException` (the monitor swallows it
  and returns empty, `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:87-90`); `Load(null)` under
  the default inline lifetime; assert every update-declaring card keeps `CanUpdate == true` with
  a null `Message` (fail-open settle). Optionally extend `CreateViewModel` (lines 37-73) with an
  optional `ILogger<AgentProcessMonitor>?` parameter to build the monitor with a mock logger and
  verify the monitor logged the failure (design D7 / open question 4); the pass-level backstop
  catch is **not** directly testable through existing seams — do not invent one.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green: the seven unchanged pins plus the four
new tests.

## Stage 2: Spec delta alignment, backlog, and final verification

The delta already exists under `openspec/changes/async-blocked-state/specs/agent-ui/spec.md`;
correct it in place, do not recreate it.

- [ ] 2.1 Re-read `openspec/specs/agent-ui/spec.md` (requirement "Agent card action availability",
  lines 219-246) against the implemented code from Stage 1 and correct any statement in the delta
  that does not match. All five original scenario names and bodies stay verbatim; the five new
  scenarios ("Blocked state populates after the cards render", "Transient declarative
  availability while the blocked state is pending", "Reload supersedes the blocked-state pass",
  "Surface exit cancels the blocked-state pass", "Blocked-state enumeration failure is logged")
  must stay accurate — in particular: the transient scenario's execution-time clause (choosing
  "Update" still does not start the update) is the `AgentManagementService.UpdateAsync` gate
  (`AgentManagementService.cs:76-83`), and the surface-exit scenario must say the enumeration
  "may complete" but is "not applied" — nothing is cached (design Discrepancy 1).
- [ ] 2.2 `docs/backlog.md`: replace only the "Async blocked-state population on the agents page"
  entry (lines 20-35) with a resolved entry in the format of "Version population loop
  consolidation (resolved)" (lines 62-85): Status — resolved by the `async-blocked-state` change
  (record the implementation date); Resolved model — cards build with `CanUpdate =
  declaresUpdate`; a sibling blocked-state pass in the canonical shape (own CTS/generation,
  operation name "Agent blocked-state population") enumerates via `Task.Run` off the UI thread
  and writes `CanUpdate` per card plus the blocked `Message` only when blocked; reload/page-exit
  cancels the pass and in-flight enumerations complete without applying; enumeration failures
  log through the monitor and settle fail-open; the `SettingsWindow.ShowSettings` restructure
  listed in the parked remedy was evaluated and dropped as unnecessary (the pass removes the
  load-blocking cost; load-before-show serves every page). Guarded by: the four new tests from
  1.7-1.10 plus the seven unchanged pins. Trigger: a regression report that the blocked state
  never lands or lands on a closed surface, or any future edit that adds cost to the
  running-agent enumeration path. The file carries two unrelated pre-existing uncommitted
  entries ("`pipeline-state check` cannot validate commit shas from foreign repositories" and
  "Resolver role override ignores exhaustedUntil") — rewrite only this entry and leave every
  other line untouched.
- [ ] 2.3 Confirm `docs/architecture/processes.md` needs no update (it describes the monitor,
  matcher, inspector pre-filter, and launch-window gating — all untouched; it does not describe
  the management page's population). Update it only if Stage 1 changed something it states.
- [ ] 2.4 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors.
- [ ] 2.5 Run `dotnet test CLIHub.sln` — every suite green; report the per-project counts.
- [ ] 2.6 Run `openspec validate --all` — every item passes; quote the output.
- [ ] 2.7 Run `graphify update .` from the repository root so the graph reflects the new pass.
- [ ] 2.8 Manual smoke (user; blocks commit and archive per the repository policy): with at least
  one agent's process running, open Settings → CLI Agents — cards must appear immediately, the
  running agent's Update briefly available, then blocked with the "Update blocked while the
  agent is running: …" explanation; click Update during the transient and confirm the update
  does not start and the card reports the block; close Settings immediately after opening and
  confirm no "Agent blocked-state population" errors appear in the current day's log
  (`%APPDATA%\CLIHub\logs\`); re-open Settings and confirm the state settles again.
- [ ] 2.9 Report the exact gate outputs, any deviation, and the files changed for review. Do not
  commit; ask the user about archiving and committing.

**Final gate:** every step above passes or is explicitly reported as not run.
