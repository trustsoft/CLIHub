# Design

## Verified Current State

All citations verified against the working tree at `fcbb507` (only `docs/backlog.md` carries
unrelated pre-existing edits — two parked entries from another workstream, untouched by this
change). The backlog entry (`docs/backlog.md:20-35`) is **not drifted** on its core evidence; two
of its remedy clauses need correction — see "Discrepancies".

### The synchronous pass being moved (`AgentManagementViewModel.Load`)

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, the Settings → CLI Agents page
(DI transient behind `IAgentManagementSection`, `src/CLIHub/Composition/ServiceRegistration.cs:165`).

- `Load` (`:179-227`) computes `anyDeclaresUpdate` over the plugins (`:195`), and when any plugin
  declares an update command runs **one** synchronous enumeration on the UI thread —
  `this.processMonitor.GetRunningAgents().ToLookup(agent => agent.Plugin.Id)` (`:196-198`) — then
  builds each card with `CanUpdate = declaresUpdate && runningAgents.Count == 0` (`:217`) and
  `Message = "Update blocked while the agent is running: {blockedNames}"` (`:218`). The intent
  comment (`:190-194`) documents the Load()-time snapshot semantics and the D1 grouping
  equivalence. `PopulateVersions()` starts at the end of `Load` (`:226`).
- The grouping is fix-settings-open-delay D1: one enumeration, partitioned client-side by plugin
  id, is exactly the per-plugin result set (`AgentProcessMonitor.GetRunningAgents(plugin)` is
  `GetRunningAgents().Where(id matches)`, `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:100-107`).
- The zero-work guard: when no plugin declares an update command, no enumeration runs at all
  (`:195-198`) — pinned by `Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:495-513`).

### The canonical population shape (landed by `consolidate-version-population`, 2026-10-24)

Same class:

- Fields `versionPopulationCts` / `versionPopulationGeneration` (`:33-34`).
- `PopulateVersions` (`:265-278`): cancel-and-dispose the old CTS, create a new one, bump the
  generation, capture the token, snapshot `this.Cards.ToList()`, start
  `_ = this.RunAsync("Agent version population", applicationToken => this.PopulateVersionsAsync(
  cards, generation, refreshCancellationToken, applicationToken))`.
- `PopulateVersionsAsync` (`:280-322`): links refresh + application tokens (`:286-289`), probes
  per card in parallel, writes only under `!cancellationToken.IsCancellationRequested &&
  generation == this.versionPopulationGeneration` (`:296`, `:304`, `:310`), local silent-OCE catch
  (`:315-317`) plus `LogError` catch (`:318-321`).
- `Dispose` (`:230-237`): bumps the generation, cancels/disposes/nulls the CTS, then sets
  `disposed` (the flag stays load-bearing for the `Load`/`DetectAsync` early-returns, `:181-184`,
  `:345-348`).

The pane's equivalent (`src/CLIHub/ViewModels/Agents/AgentPaneController.cs`) is the reference the
consolidation aligned to: CTS block `:92-96`, `RunAsync` start `:112-118`,
`PopulateVersionsAsync` `:164-193`, `Dispose` `:149-162`.

### The fire-and-forget boundary (why `Task.Run` is mandatory here)

- `ApplicationOperationLifetime.RunAsync` (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:36-53`)
  starts the operation **inline on the caller's thread** — `RunObservedAsync` simply awaits
  `operation(this.cancellation.Token)` (`:96`); there is no thread switch. The version pass's
  first move is an `await` on a genuinely asynchronous service call, so it leaves the UI thread
  immediately.
- `AgentProcessMonitor.GetRunningAgents()` (`AgentProcessMonitor.cs:65-93`) is a **synchronous**
  method. A blocked-state pass that called it directly would run the entire enumeration on the UI
  thread — the change would be a latency no-op. The pass must wrap it in
  `Task.Run(() => this.processMonitor.GetRunningAgents(), cancellationToken)`.
- After that `await`, the continuation resumes on the captured dispatcher context (the await
  began on the UI thread), so the card writes land on the UI thread — the same property the
  version pass's writes already rely on.

### What the enumeration costs and how it fails

- `GetRunningAgents()` derives candidate process names from the catalog's launch executables
  (`AgentProcessMatcher.GetCandidateProcessNames`,
  `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:27-44` — exactly the matcher's
  comparison basis, `:57-68`) and calls
  `IAgentProcessInspector.GetRunningProcesses(candidateProcessNames)` (`AgentProcessMonitor.cs:72-73`).
- `WindowsAgentProcessInspector.GetRunningProcesses`
  (`src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:28-111`): one `Process.GetProcesses()`
  snapshot (`:36`), PID < 100 skip (`:43-46`), candidate-name pre-filter **before** any expensive
  read (`:54-57`), then `MainModule.FileName` (`:63`) and `StartTime` (`:81`) for candidates only.
  Measured on the dev machine during fix-settings-open-delay: full pass 851 ms → filtered 20 ms.
- Failure behavior: the monitor catches **everything**, logs through its injected
  `ILogger<AgentProcessMonitor>` (`"Failed to check for running agents"`,
  `AgentProcessMonitor.cs:87-90`), and returns an empty list — callers cannot distinguish
  "nothing running" from "enumeration failed". Cards therefore settle fail-open
  (`CanUpdate` stays true), in sync and async alike. The monitor is **cacheless** — nothing
  persists between calls.

### The safety net that makes the transient acceptable

- Execution-time gate: `AgentManagementService.UpdateAsync`
  (`src/CLIHub.Core/Agents/AgentManagementService.cs:64-102`) re-enumerates synchronously and
  returns `AgentUpdateOutcome.Blocked` with "Update blocked while the agent is running: {names}"
  when matching processes exist (`:76-83`). The VM surfaces that message on the card
  (`AgentManagementViewModel.UpdateAsync`, `:364-380`).
- The launch window's gating is independent and synchronous per requery:
  `WindowActionCoordinator.CanExecuteUpdate` (`src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-161`)
  and `ExecuteUpdate`'s double-check (`:163-182`). Untouched by this change.
- The pane row's `CanUpdate` is purely declarative — `Plugin.Commands?.Get(AgentCommandKind.Update)
  != null` (`src/CLIHub/ViewModels/Agents/AgentItem.cs:79`) — which is the vocabulary this change
  adopts for the card's build-time value.

### The Settings open path

- `SettingsLauncher` reuses one window; every activation calls `ShowSettings`
  (`src/CLIHub/Settings/SettingsLauncher.cs:26-35`).
- `SettingsWindow.ShowSettings` runs `this.viewModel.Load()` **before** `this.Show()`
  (`src/CLIHub/Views/SettingsWindow.xaml.cs:38-62`, load at `:40`, show at `:54`). Settings close
  disposes the view model (`:30`), and re-activation re-`Load`s it — so re-entrant `Load` on a
  live view model is a real production path, and supersession is not theoretical.
- `SettingsViewModel.Load` fans out to `this.AgentsState.Load(draft.DefaultAgentId)` at the end of
  a long synchronous sequence (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:1313`, `:1388`).
- The Update button binds `IsEnabled="{Binding CanUpdate}"`
  (`src/CLIHub/Views/SettingsAgentsPage.xaml:201`); the message line binds `Message`/`HasMessage`
  (`:175-178`). Neither needs a XAML change.

### Specs

- `openspec/specs/agent-ui/spec.md:219-246` — "Agent card action availability", with scenarios
  "Test always available" (`:223`), "Update requires the update command" (`:228`),
  "Update blocked by a running agent" (`:233`), "Other running agents do not block an update"
  (`:238`), "Edit Config requires a plugin file" (`:243`). The blocked scenario is **state-based**
  ("WHEN a process matching the agent being updated is running THEN 'Update' is unavailable …"),
  which is exactly what the transient strains.
- `openspec/specs/preferences-ui/spec.md:535-567` — "Agent card actions": "Update blocked while
  running" (`:549-552`) is **action-based** ("WHEN the user chooses 'Update' …") and is satisfied
  by the execution-time gate regardless of card state; "Action availability reflects capability"
  (`:564-567`) matches the declarative rule. "Settings window access" (cited at
  `openspec/changes/archive/2026-10-20-fix-settings-open-delay/design.md:38-39`) promises the
  window "opens showing the current preferences".
- `openspec/specs/agent-version/spec.md:97-121` — "Stale version population does not update the
  current list": a cancelled pass "starts no further version lookups or availability checks"
  (`:99`). Its wording covers the version pass only — the reason the blocked-state pass must not
  join it (D1).
- `REQ-AGENT-CAP-002` ("Update Command Support Detection", `agent-ui/spec.md:9-25`) checks
  `CanUpdate` against the declared command. It is already read today as declarative *support*
  detection (a running agent overrides `CanUpdate` to false in the current code too), so the
  async change does not alter what it describes; it is not modified.
- `docs/architecture/processes.md:11-33` describes the monitor, matcher, inspector pre-filter, and
  the launch-window gating; it does not describe the management page's population, so no doc
  change is required by this change.

### Test topology

- `AgentManagementViewModelTests.CreateViewModel`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:37-73`) builds a **real**
  `AgentProcessMonitor` over a mocked `IAgentProcessInspector` and defaults to
  `InlineOperationLifetime` (`:71`) — which invokes the operation and blocks on it
  (`:571-583`), so a fire-and-forget pass completes synchronously inside `Load` and every
  settled-state assertion holds unchanged. `DeferredOperationLifetime` (`:585-593`) invokes
  without blocking, for pending-pass scenarios.
- Existing blocked-state pins: `Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock`
  (`:418-436`), `Load_OtherAgentsProcessRunning_KeepsUpdateAvailableWithoutMessage` (`:438-458`),
  `Load_WithNoRunningProcesses_KeepsDeclaredUpdateAvailableWithoutMessage` (`:460-472`),
  `Load_MultipleUpdateDeclaringPlugins_EnumeratesProcessesOnce` (`:474-493`),
  `Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses` (`:495-513`).
- `AgentProcessMonitor` is a concrete, non-virtual class the view model takes directly
  (`AgentManagementViewModel.cs:28`, `:55`) — it cannot be Moq-intercepted, and it swallows
  enumeration failures internally, which constrains how the failure paths can be tested
  (D7).

## Decisions

### D1: A sibling pass with its own CTS/generation — not merged, not composite

`Load` starts a second fire-and-forget pass, `PopulateBlockedState`, structurally identical to
`PopulateVersions` but with its own `blockedStateCts`/`blockedStateGeneration` and the operation
name "Agent blocked-state population". `Dispose` cancels both; a re-`Load` supersede-restarts
both.

**Rationale:**

- Same lifecycle, different shape. Both passes start at `Load`, are superseded by a re-`Load`, and
  are cancelled at surface exit — but the version pass is parallel per-card probing of two async
  services, while the blocked-state pass is one synchronous enumeration wrapped in `Task.Run`
  followed by per-card writes. The canonical shape is per-pass-purpose; a second instance is
  mechanical, and the just-archived consolidation change deliberately did *not* extract a shared
  engine (its D3) — this change honors that by repeating the shape, not inventing an abstraction.
- Failure-domain and log separation. A fault in one pass must not skip the other's writes; the
  operation names ("Agent version population" is shared with the pane,
  `AgentPaneController.cs:113`) must stay distinguishable in the log.
- Spec boundaries. The `agent-version` requirement enumerates what a cancelled pass must not
  start — "version lookups or availability checks" (`agent-version/spec.md:99`). Folding the
  process enumeration into the version pass would make that recorded contract under-describe the
  pass (or force another `agent-version` delta). A sibling pass keeps `agent-version` untouched
  and puts every new obligation where it belongs — the `agent-ui` delta this change must write
  anyway.
- The `anyDeclaresUpdate` zero-work guard is foreign to version population (which always runs);
  keeping the passes separate keeps that property scoped to the pass that owns it.

### D2: Declarative interim — cards build with `CanUpdate = declaresUpdate`, no message

The "placeholder treatment for the Update action" is the declarative value itself, exactly
parallel to the `"…"` version placeholder: cards render with Update available precisely when the
agent declares the command, and the blocked state (when any) arrives when the pass lands. No new
UI states, no XAML change.

**Rationale:**

- It is the parked remedy's own shape (`docs/backlog.md:27-29`) and matches the pane row's
  declarative `CanUpdate` (`AgentItem.cs:79`).
- It preserves "Update requires the update command" (`agent-ui/spec.md:228-231`) byte-for-byte at
  render time and after settlement.
- The transient mis-shows only for cards whose agent actually runs. The pessimistic alternative
  (disable Update for every update-declaring card until the pass lands) would transiently violate
  "Other running agents do not block an update" (`agent-ui/spec.md:238-241`) for *all*
  update-declaring agents — a visible regression for the common no-agents-running case, in
  exchange for hiding a rarer one.
- Safety is unchanged: the execution-time gate blocks the click and the card reports it
  (`AgentManagementService.cs:76-83`) — the same outcome a user gets today whenever the Load-time
  snapshot has gone stale, which can happen the instant the synchronous pass completes.

### D3: Pass write rules — `CanUpdate` always, `Message` only when blocked, never cleared

When the enumeration completes (under the token+generation guard), the pass writes
`card.CanUpdate = declaresUpdate && running.Count == 0` for the snapshot's cards and writes
`Message = "Update blocked while the agent is running: {names}"` **only** for blocked cards. It
never sets `Message = null`.

**Rationale:** non-blocked cards already carry the null message from construction, and a
user-action message (`Test`/`Update` outcomes, `AgentManagementViewModel.cs:330-332`, `:366-372`)
can in principle land between render and pass completion; a clearing write could clobber it.
Write-only-when-blocked is today's build-time behavior shifted into the pass. The pass does not
touch `RefreshAvailabilitySummary` or `RebuildDefaultAgentOptions` — `CanUpdate`/`Message` feed
neither (unlike `Version`).

### D4: The enumeration runs inside `Task.Run`; the linked token governs application, not abort

`await Task.Run(() => this.processMonitor.GetRunningAgents(), cancellationToken)` moves the
snapshot + candidate reads off the UI thread (mandatory — see Verified Current State: `RunAsync`
starts inline on the caller's thread). `Task.Run`'s token only prevents the delegate from
*starting*; `GetRunningAgents()` accepts no token, so an in-flight enumeration runs to completion
on the pool and its result is discarded by the `!cancelled && generation == current` guard — the
same "may complete, is not applied" semantic as an in-flight version probe, minus the cache (the
monitor is cacheless; the "Monitor-level running-agent cache" item stays parked).

### D5: `SettingsWindow.ShowSettings` is NOT restructured — the backlog consequence is challenged

The backlog entry says the load-before-show order "would need restructuring too"
(`docs/backlog.md:32`). Dropped:

- The restructure was a consequence of D2 as originally scoped — an async *Settings load*. Scoped
  to the blocked-state pass, `Load`'s agents-page cost drops to the config parse, per-plugin
  detection, and declarative card building; the window shows promptly with load-before-show
  intact.
- Load-before-show is what makes the window "open showing the current preferences"
  (preferences-ui "Settings window access") for **every** page; show-before-load would flash
  stale or empty state across the whole Settings surface — far beyond this capability.
- Residual synchronous `Load` cost (settings parse, detection, `ProjectRegistry.Load`) is
  unchanged and out of scope. If Settings open is still perceived slow after this change, that is
  a new, separately-scoped backlog item.

The resolved backlog entry (task 2.2) records this correction so the parked text does not mislead
a future reader.

### D6: Spec delta — one MODIFIED `agent-ui` requirement; the blessing lives in the requirement text

"Agent card action availability" is modified: all five original scenario names and bodies are
preserved **verbatim** (they describe the settled state, which is unchanged), and the requirement
text is extended to (a) define that the state-based scenarios apply once the blocked-state pass
has completed for the current list, (b) explicitly permit the transient — including that choosing
"Update" during it still does not start the update, (c) require the pass to be cancelled on
reload and surface exit with in-flight results not applied, (d) require no enumeration when no
agent declares an update command, and (e) require failures to be logged with the cards keeping
declarative availability. Five new scenarios carry the new obligations: "Blocked state populates
after the cards render", "Transient declarative availability while the blocked state is pending",
"Reload supersedes the blocked-state pass", "Surface exit cancels the blocked-state pass",
"Blocked-state enumeration failure is logged" — mirroring the structure the
`consolidate-version-population` delta gave `agent-version`.

**No other spec changes:** preferences-ui's blocked scenario is action-based and stays satisfied
by the execution-time gate; its "Settings window access" promise still holds (the window opens
showing the stored preferences — the blocked state is live process-derived state, not a stored
preference, and is not pinned to the open instant even today). `agent-version` is untouched
(D1). `REQ-AGENT-CAP-002` already reads as declarative support detection and is not modified.

### D7: Zero-work guard and failure semantics preserved exactly; one backstop catch is untestable by seam

- `Load` keeps computing `anyDeclaresUpdate` (the plugins are already enumerated there) and starts
  the blocked-state pass only when true — preserving "plugins without an Update command never
  trigger an enumeration" and the two enumeration-count tests unchanged.
- Enumeration failure is the monitor's own swallow: logged through its `ILogger<T>`
  (`AgentProcessMonitor.cs:87-90`) with an empty result, so the cards settle fail-open at
  declarative availability — the same observable outcome as today's synchronous pass. The delta's
  failure scenario is worded at this observable level and is directly testable (inspector mock
  throws; assert the settle and, via a monitor-logger seam, the log).
- The pass itself still carries the canonical local catches (silent surface-local OCE;
  `LogError("Agent blocked-state population failed")`). Directly unit-testing that backstop is
  not possible through existing seams — `AgentProcessMonitor` is concrete and non-virtual and
  swallows its own failures, so nothing the view model owns can be made to throw between the
  `Task.Run` and the writes. It is kept for shape consistency with the canonical pass (and
  protects future edits, e.g. someone adding a step that can throw), with the operation-lifetime
  runner as the further backstop. Recorded here so the implementation session does not invent a
  seam for it.

## Alternatives Considered

### Merge the enumeration into `PopulateVersionsAsync` (enumeration first, then probes)

**Rejected** — see D1: conflates failure domains (an enumeration fault would skip version
fill-in), muddies the pane-shared "Agent version population" operation name, drags the
`agent-version` requirement into scope, couples the blocked-state arrival to the version pass's
structure, and would contradict the model `consolidate-version-population` just recorded.

### One composite "agent state population" pass replacing both

**Rejected** — same problems as merging, plus it renames and rewrites the version-population
shape that change landed and spec-recorded days ago, invalidating its documented model and its
delta wording for zero user-visible gain.

### Shared CTS, separate generations

**Rejected** — incoherent: the canonical starter does cancel-and-replace, so the second starter
(`Load` starts both passes) would cancel the first pass's token. A single combined start method
is just the composite option again.

### Pessimistic interim (`CanUpdate = false` until the pass lands)

**Rejected** — see D2: transiently violates "Other running agents do not block an update" for
every update-declaring agent; the declarative interim mis-shows only actually-blocked cards, and
is what the parked remedy prescribes.

### Visible busy/placeholder treatment for the Update button (disabled + "Checking…" text)

**Rejected** — new UI states, new visuals, a wider delta, and a new card contract, against the
parked remedy's minimal shape ("new UI states, new spec text" is exactly what fix-settings-open-delay
D2 flagged as the cost). Revisit only if users report the transient as confusing; the pre-filter
makes the window tens of milliseconds wide.

### Restructure `ShowSettings` to show before load

**Rejected** — see D5.

### Refresh the blocked state on `Test`/`Detect`/`Update` or process-start events

**Rejected** — out of scope; `Load` remains the refresh trigger (the comment at
`AgentManagementViewModel.cs:190-194` is updated in place, not re-scoped). The execution-time
gate already covers action-time safety, and process-start events would need a monitoring
subscription — a different capability.

## Discrepancies

1. **"A running-agent enumeration already started may complete and cache (AgentProcessMonitor)" —
   the cache clause is wrong.** The monitor is cacheless (`AgentProcessMonitor.cs:65-93`; the
   monitor-cache item is parked precisely because caching would stale the execution gate,
   `docs/backlog.md:37-48`). An in-flight enumeration may *complete*, but nothing is cached —
   only the application to the abandoned surface is prevented (token + generation guard). The
   "complete and cache" phrasing is true of version probes (`AgentVersionService` runs them on
   `CancellationToken.None` and caches), not of this pass. The delta's surface-exit scenario is
   worded accordingly.
2. **"`SettingsWindow.ShowSettings` … would need restructuring too" — challenged and dropped**
   (D5). The consequence held only for the whole-Settings-async reading of D2; the resolved
   backlog entry corrects it.
3. **Backlog evidence is otherwise current.** `Load` does run the enumeration synchronously on
   the UI thread (`:195-198` at `fcbb507`); the pass is one snapshot plus the candidate pre-filter
   (`AgentProcessMonitor.cs:72-73`, `WindowsAgentProcessInspector.cs:54-57`), cheap but not free.
   The remedy's "`PopulateVersions` shape" is now literally `PopulateVersions`/`PopulateVersionsAsync`
   after the consolidation — still the intended shape.
4. **The brief's "placeholder treatment" is interpreted as the declarative interim** (D2), not a
   new visual state — the backlog's own remedy sentence ("build cards with
   `CanUpdate = declaresUpdate`") fixes the interim value, and fix-settings-open-delay D2 prices
   new UI states as a separate cost.

## Open Questions

1. **Confirm dropping the `ShowSettings` restructure** (D5). Default: drop — the async pass
   removes the load-blocking cost and load-before-show serves every other page. Say so before
   Stage 1 if you want the restructure scoped in anyway.
2. **Confirm the transient direction** (D2): declarative interim (recommended, backlog-prescribed)
   vs pessimistic interim. Default: declarative.
3. **Operation name** "Agent blocked-state population" (mirrors "Agent version population"). Any
   preference, speak before Stage 1; trivial to change later but it lands in logs and tests.
4. **Monitor-logger seam in tests** (D7): task 1.10 extends `CreateViewModel` with an optional
   `ILogger<AgentProcessMonitor>` so the enumeration-failure test can assert the monitor's log
   entry. If you prefer the test to assert only the fail-open settle (no logger seam), drop that
   clause — the observable card behavior is the part the spec pins.

No question blocks implementation.
