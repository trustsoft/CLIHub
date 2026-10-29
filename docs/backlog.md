# Backlog

Parked findings and deferred work. Each item records evidence, severity, a proposed remedy, and the
trigger that should bring it back. The pipeline protocol (`pipeline-protocol` skill) reviews this
file during `/pipe-resume`; do not re-derive parked work from conversation history.

## Architecture and design

### Architectural improvement proposals (2026-10-29 analysis)

- Severity: Medium (strategic, not urgent; affects long-term maintainability and evolution)
- Evidence: comprehensive architectural analysis completed 2026-10-29 (commit `e141732`), documented
  in `docs/architecture-analysis-2026-10-29.md`, `docs/architecture-visualization.md`, and
  `docs/architecture-improvement-summary.md`. Analysis identified 6 concrete improvement proposals:
  (1) Extract cross-cutting services (logging, persistence, caching); (2) Introduce domain events;
  (3) Capability-based service decomposition; **(4) Process domain model unification [IMPLEMENTED 2026-10-29]**;
  (5) Plugin lifecycle management; (6) Configuration decomposition (internal). Health score: 8.5/10. Key
  concerns: coordinator proliferation (5 coordinators), large role-based service interfaces,
  UI-heavy orchestration, configuration coupling.
- Proposed remedy: Three-phase evolution roadmap (Q1-Q3 2027): Phase 1 (foundation) — cross-cutting
  services and domain events; Phase 2 (domain refinement) — service decomposition, **process domain [ALL PHASES COMPLETE 2026-10-29]**,
  plugin lifecycle; Phase 3 (configuration evolution) — internal decomposition. Immediate actions
  (next 30 days): document architecture principles, extract `ILoggingContextProvider`, implement
  first domain event POC (`ProjectSelected`). See `docs/architecture-analysis-index.md` for full
  navigation. **Proposal 4 complete: Phase 1+2 `a64422c`, Phase 3 `37e3c46`, Phase 4 `f7c1495` (obsolete
  type removed, 0 CLIHUB001, 1,222 tests); see ADR-0012 and the archived changes.** **Proposal 1 first
  slice complete: `logging-context-provider` `7d92bf3` (ILoggingContextProvider + OperationName/CorrelationId
  scopes on orchestrator, monitor, application lifetime, and scanning; behavior unchanged, 1,228 tests).
  Remaining Proposal 1 slices: `IPersistenceService<T>` and `ICacheService` (deferred; triggers: second
  consumer of a hand-rolled persistence/caching pattern, or a storage-backend change).** **Proposal 1
  remaining slices CLOSED BY DECISION (2026-10-29): no generic `IPersistenceService<T>` or
  `ICacheService` - reconnaissance found the shared atomic-write code is two statements while the
  surrounding policies diverge (config: debounced writer + locks + generation; logo cache: dispose-time
  write + dirty flag), and the agent caches share only a two-line TTL check. Recorded as ADR-0013 with
  reopen triggers (third matching consumer, a config-format-v2 need for a backend abstraction, or a
  fourth same-shaped agent cache). Investigation archived as `2026-10-29-persistence-service-slice`.** **Proposal 2 POC
  complete: `domain-events-poc` (synchronous domain event bus + `ProjectSelected` event published from
  `ProjectService.SetCurrentProject` + logging subscriber using `ILoggingContextProvider`; zero
  user-visible behavior change, 1,240 tests). Adoption catalogue pending Proposal 2 follow-up.** **Proposal 2
  catalogue complete: `domain-events-adoption` `49f6511` (`docs/architecture/domain-events.md` adoption
  rules + candidate triggers; `AgentLaunched` on Launch/Resume with audit subscriber; lazy-singleton
  subscriber activation fixed and pinned; 1,248 tests, smoke confirmed). Remaining proposals 3, 5, 6 open.**
  **Proposal 3 first slice complete: `capability-service-slice` `774d468` (IAgentManagementService split
  into IAgentProbeService + IAgentUpdateService, `AgentManagementViewModel` composes both; zero behavior
  change, 1,248 tests). Further decompositions deferred until consumer/test subset-use evidence appears.**
- Trigger: Q1 2027 planning cycle, onboarding new contributors, or next major refactoring that would
  benefit from architectural clarity.

### Configuration decomposition (Proposal 6) - first slice in progress

- Severity: Low (structural, no user impact expected)
- Evidence: analysis of 2026-10-29 flagged the single `AppConfig` as coupling unrelated concerns
  (preferences, projects, scanning) while ADR-0002 pins one camelCase document with one atomic write.
  The document layer already splits sections (`PreferencesDocument`, `ProjectStateDocument`, scanning
  settings), so part of the intent may already be satisfied - the slice must prove real coupling
  before restructuring anything.
- Proposed remedy: `config-decomposition-slice` change - internal domain-aligned decomposition at the
  snapshot/repository boundary only; external file format, migrations, and atomic write stay untouched.
  Reject or shrink if reconnaissance shows the coupling is already contained. **Result (2026-10-29,
  commit `de590e4`, archived as `2026-10-29-config-decomposition-slice`): `ScanningPreferences` value
  object extracted (five flat `AppPreferences` properties composed as `Scanning`), document mapping
  delegates the section but keeps the identical flat camelCase wire fields, snapshot copies via
  `Scanning.Copy()`; census/round-trip/deep-copy pins in place; 1,249 tests. Deferred follow-ups:
  Agent/Update/Appearance extraction and store narrowing, each with an evidence trigger.**
- Trigger (reopen): a further cross-concern edit that still forces synchronized mapping changes, or
  the `config-format-v2` decision below.

## Agent and process domain

### Detect failure restores install state but not the version (low, deferred)

- Severity: Low — exception path only, no notification fires there, and it self-heals on the next
  successful pass.
- Evidence: `DetectAsync` snapshots only `InstallState`
  (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:437`) and, on a thrown probe, restores
  that state for every flipped card (`:462-472`); cards that had already settled keep the
  `Version`/`UpdateAvailability` the abandoned pass wrote. `DetectCardAsync` has the same family
  (`:422-428` with `ApplyProbeResult` `:490-503`). `DefaultAgentDetail` (`:110-125`) reads only
  `Version`, so after a later selection change a card restored to NotInstalled can pair
  "(Not installed)" in the selector with "Version 2.0" in the summary. The spec scenario "Detecting
  never persists after a failed probe" is satisfied.
- Proposed remedy: snapshot `Version` and `UpdateAvailability` together with `InstallState` and
  restore all three on a thrown pass, or revert only the cards still `Detecting` and leave settled
  cards wholly settled; extend the existing restore tests to the chosen rule.
- Trigger: the next change touching `AgentManagementViewModel`'s probe paths, or a report of a
  version line beside an uninstalled agent after a failed Detect.

### Dispose racing a Detect pass can leave unprobed cards in Detecting (low, deferred)

- Severity: Low — the surface is torn down, and `Load()` rebuilds the cards, so nothing renders the
  residual state.
- Evidence: `DetectAsync` returns early when `this.disposed`
  (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:451-454`); cards flipped to
  `Detecting` at pass start (`:441-444`) that were never probed keep that state, because the
  restore path runs only for exceptions.
- Proposed remedy: treat the early return like a cancelled pass — restore the snapshot before
  returning, or route it through the exception restore path.
- Trigger: reuse of a disposed or abandoned section instance, or a future surface that keeps cards
  alive across a cancelled Detect.

### A failed manual update check leaves no summary and stale indicators (low, deferred)

- Severity: Low — exception path only; the next successful check or a page reload heals it.
- Evidence: `CheckUpdatesAsync` clears `UpdateCheckSummary` at the start and only rebuilds it after
  `Task.WhenAll` (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:518,545-550`), so a
  thrown `CheckAsync` leaves the line empty; cards checked before the failure keep their fresh
  results, cards after it keep the previous (possibly stale) advisory indicators. `InvalidateFor`
  has already dropped their cached results, so the next press re-fetches. The failure is logged by
  the operation lifetime (`ApplicationOperationLifetime`, "Application operation failed").
- Proposed remedy: on a thrown pass, either restore the pre-pass indicators for every card or set a
  short failure summary (for example "Update check failed") so the empty line cannot read as "no
  updates"; cover the half-finished pass in a test.
- Trigger: a report of a silent or ambiguous manual update check, or the next change touching
  `CheckUpdatesAsync`.

### Runtime integration tests pass silently when the runtime is absent

- Severity: Low
- Evidence: `LaunchAsync_GitBash_ChildStartsInWorkingDirectory` and
  `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory`
  (`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`) `return` early —
  reported as passed, not skipped — when the runtime cannot be resolved on the machine.
- Proposed remedy: use a skip mechanism (for example `SkippableFact`) so CI reporting distinguishes
  "not run" from "passed".
- Trigger: a CI coverage review or a hidden-regression investigation.

## DI and lifetime

### Transient IDisposable chain retained by the root provider

- Severity: Low
- Evidence: each Settings open resolves a transient IDisposable chain from the root provider, and
  MS.DI retains transients until application exit.
- Proposed remedy: resolve through a factory that disposes explicitly, if memory profiles ever show
  growth.
- Trigger: a memory-growth report.

## Test suite

### Single unexplained test failure (not reproduced)

- Severity: Low (watch item)
- Evidence: during pipeline verification of `git-bash-resolution` on 2026-10-20, one run of
  `CLIHub.Core.Tests` reported 1 failed / 553 passed and the failing test name was not captured.
  Five subsequent runs were fully green, as were the implementer's and independent verifier's runs.
  Likely candidates are the pre-existing timing-sensitive tests: `UpdateCheckerTests` and
  `UpdateDownloaderTests` (100 ms cancellation windows) and `ProjectScanningServiceTests`
  (stopwatch-bounded wait loop). During the `scanning-flake-settle` fix (2026-10-24), the captured
  recurring flake was exactly `ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus`
  (see its resolved entry), making it the probable identity of this incident too; the
  `UpdateDownloaderTests` (100 ms cancellation window) candidate stays unexcluded until a captured
  failure names it (`UpdateCheckerTests` was deleted with `remove-unused-code-tier1`).
- Proposed remedy: on recurrence, re-run with `--logger trx`, capture the test name, and stabilize the
  offending timing window.
- Trigger: any red `dotnet test CLIHub.sln` without a matching code change.

### CheckCardUpdatesCommand test races the card-action reset (low, flaky)

- Severity: Low (test-only race; not a product bug)
- Evidence: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:1609`
  (`Assert.False(viewModel.IsCardActionRunning)`) failed once on 2026-10-30 during verification of
  `remove-unused-code-tier1` (Expected False, Actual True), then passed in isolation and in five
  repeat runs. The `SpinWait` predicate (`AgentManagementViewModelTests.cs:1605`,
  `CheckCardUpdatesCommand.CanExecute(cardA)`) becomes true at `AgentManagementViewModel.cs:725`
  (`card.IsCheckingUpdates = false`) *before* `ExitCardAction()` clears `IsCardActionRunning` at
  `:726`, so the assertion can observe that intermediate state under parallel load. Not caused by
  that change: neither file is in its diff.
- Proposed remedy: extend the `SpinUntil` predicate to also require `!viewModel.IsCardActionRunning`,
  or spin on `IsCardActionRunning` before the final assertion.
- Trigger: the next red run of
  `CheckCardUpdatesCommand_WhilePerCardCheckRuns_DisablesOnlyTheActingCardAndGatesHeader`, or a CI
  flake carrying that test name.

### ApplicationHost null-argument tests fail while the app is running

- Severity: Low (test isolation; the suite result depends on machine state, not on the code under test)
- Evidence: `tests/CLIHub.Tests/ApplicationHostTests.cs:27` (`Start_WithNullDispatch_ThrowsArgumentNullException`)
  and `:40` (`Start_WithNullSetMainWindow_ThrowsArgumentNullException`) report
  `Assert.Throws() Failure: No exception was thrown` whenever a CLIHub instance is running. Reproduced on
  2026-10-23 during the `accent-independent-of-theme` smoke: with the app open `CLIHub.Tests` reported
  410 passed / 2 failed; after closing it, 412 / 412 passed with no code change. The single-instance
  guard short-circuits `ApplicationHost.Start` (a second instance exits before the argument checks), so
  the null-argument guard is never reached.
- Proposed remedy: make single-instance ownership injectable into `ApplicationHost` (or bypass it in a
  test mode) so the argument validation is exercised regardless of a running instance, then assert both
  the null-argument exceptions and the ownership path explicitly.
- Trigger: the next red `CLIHub.Tests` run that coincides with the application being open, or any CI job
  that runs the suite on a machine where CLIHub autostarts.

### ApplicationHost second-instance activation test times out on CI (low, deferred)

- Severity: Low (CI flake; the same test passes locally and on the next commit).
- Evidence: CI run `40226957693` (commit `7355819`) failed
  `ApplicationHostTests.Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup`
  (`tests/CLIHub.Tests/ApplicationHostTests.cs:46-76`) with `System.TimeoutException` at `:70` after its
  5 s wait; the following commit `bdd9770` passed the same test on the same runner image. The signal is
  best-effort: `SignalActivation` makes two `Connect(500)` attempts with a 100 ms pause and swallows
  every failure (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:70-87`), while the listening server is
  started through `Task.Run` in the constructor (`:59-63`). On a cold or loaded runner the pipe server
  may not yet sit in `WaitForConnectionAsync` when the duplicate signals, so the signal is lost and
  nothing retries.
- Proposed remedy: retry the connect until an overall deadline (~2 s) instead of two fixed attempts, or
  retry signalling from the test until the activation event fires; optionally expose a server-ready
  signal for tests.
- Trigger: a second CI occurrence, or a user report that launching the app while it is already open
  does not bring the window forward.

## Agent tooling

### Reasoning effort is not weighted into the exhaustion logic

- Severity: Low (documented limitation; agent tooling only)
- Evidence: the exhaustion bookkeeping in `.opencode/scripts/resolve-model.mjs` treats every model
  as exhausted for the same backoff, while the role's `effort` level changes how much of the quota
  a dispatch consumes — `max` on a metered plan model burns it faster than `low`. The policy's
  `_effort_note` and `docs/agent-model-policy.md` ("Cost note") both state that the logic does not
  weight usage by effort.
- Proposed remedy: scale the backoff by the effort level of the dispatch that failed, so a
  high-effort failure on a plan model blocks it longer than a low-effort one.
- Trigger: a request for effort-aware quota handling, or the first plan-model exhaustion that
  recovers too slowly for the workload.

### `pipe/verifier` allowlist has no output-shaping cmdlets

- Severity: Low (degrades verifier reports, not correctness; the orchestrator re-runs every gate)
- Evidence: `~/.config/opencode/agents/pipe/verifier.md` (global user config, outside this repo)
  allows a read-only cmdlet set — `Get-Content`, `Get-ChildItem`, `Test-Path`, `Select-String`,
  `Select-Object`, `Measure-Object`, `Sort-Object`, `Format-Table`, `Format-List`, `Out-String`
  (lines 84-110) — but deliberately excludes `ForEach-Object` and `Where-Object`, because a
  scriptblock argument would let the "read-only" agent run arbitrary code. Shell permissions are
  matched per pipeline segment, so a verifier that shapes gate output with a filter/loop segment
  gets `permission.rejected` for the whole command. A second, prompt-level residual: the rule that
  a denial must be quoted verbatim instead of generalized into "shell unavailable" (`## When a
  command is denied`, line 124) is enforced by instruction only — the same failure that produced a
  false `UNVERIFIABLE` verdict on 2026-10-22 is not structurally prevented.
- Proposed remedy: either add a small allowlist of side-effect-free scriptblock-free alternatives
  (for example `Measure-Object -Line`, `Select-String`, `-First`/`-Last` on `Select-Object`, which
  are already allowed) and document them in the prompt's "Permitted commands" section, or give the
  verifier a first-class read-only inspection tool so it never needs shell plumbing for reads.
  For the reporting residual, prefer a structural fix (verifier returns a machine-readable gate
  record with per-gate status and command) over more prompt wording.
- Trigger: the next verifier report that quotes a denial for a pipeline-shaped read, or any verify
  stage where a gate ends up `NOT RUN`.

### Pipe agent definitions live in the global OpenCode config

- Severity: Medium (unversioned, unreviewed, and silently drifts per machine)
- Evidence: `pipe/planner`, `pipe/implementer`, and `pipe/verifier` are defined only under
  `~/.config/opencode/agents/pipe/` (`planner.md`, `implementer.md`, `verifier.md`), outside any git
  repository. The 2026-10-22 shell-permission fix to `verifier.md` therefore has no commit, no
  diff, no review, and no history; a fresh machine or a reinstall restores the old, narrower
  allowlist. The same applies to the pipeline contract they implement, which is documented in the
  global `pipeline-protocol` skill rather than in this repo.
- Proposed remedy: move the pipe agents (and, if the decision extends that far, the whole pipeline
  mechanism — agents, `pipeline-protocol` skill, state-file convention) to the project level under
  `.opencode/agents/` and `.opencode/skills/`, so they are versioned with the repository, reviewable
  in a diff, and covered by the same approval gates as code. Project definitions override global
  ones, so a move is reversible by deleting the project copies.
- Trigger: any further change to a `pipe/*` agent definition, a second machine/agent host that needs
  the same behaviour, or an explicit decision to version the pipeline mechanism per project.
- Decision (2026-10-22): deliberately deferred. The agents stay global for now; relocating the whole
  mechanism to project level is a later, separate decision.

### Concurrent writers can clobber `.opencode/pipeline.json`

- Severity: Medium (silent loss of pipeline state, no error surfaces)
- Evidence: the sidebar plugin only reads the file, but every orchestrator session rewrites it
  read-modify-write (read JSON, mutate, temp file + rename) with no lock or revision check. On
  2026-10-22 two sessions wrote the same file minutes apart: the theme workstream advanced
  `theme-xaml-structure` (18:21) and `live-theme-switch` (20:28) while the sidebar-limit work
  appended `pipeline-sidebar-limit`; each write preserved the other's data only because it happened
  to re-read first. A write landing between another writer's read and its rename drops that
  writer's transition silently.
- Proposed remedy: keep one writer per project (orchestrator owns the file) or serialize with a
  lock file, and make the write re-read and retry when the top-level `updatedAt` changed since the
  read (compare-and-swap). `.opencode/scripts/pipeline-state.mjs` is now the intended single writer
  and re-applies the operation when the target's `mtimeMs`/`size` changed before the rename (3
  attempts); this entry stays open until every writer uses it.
- Trigger: the first stage transition that vanishes from the sidebar, or the first two sessions
  that run pipeline work in the same project at the same time.

### `pipeline-state check` cannot validate commit shas from foreign repositories

- Severity: Low (informational warning only; `check` still exits 0 and the sidebar is unaffected)
- Evidence: `.opencode/scripts/pipeline-state.mjs:676-684` validates every recorded sha with
  `git cat-file -e` against a single repo root (`--repo`, defaulting to the state file's parent —
  the CLIHub repo). The `opencode-sidebar-suite-packaging` entry records `0185674` from
  `D:\Projects\opencode-sidebar-suite`, so `check` reports a standing `sha-missing` warning (first
  seen 2026-10-24). `update` cannot clear a sha (empty values are rejected at
  `.opencode/scripts/pipeline-state.mjs:462-464`), and a direct JSON edit would bypass the
  single-writer rule.
- Proposed remedy: support foreign-repo shas — e.g. an attested form
  (`tooling: <repo>: <sha>`) that `check` validates against the named repository, or a per-entry
  repo hint — so out-of-repo tooling commits stay traceable without a standing warning.
- Trigger: the next state entry that records a sha from a non-CLIHub repository, or any edit of the
  `pipeline-state.mjs` check path.

## UI and theming

### Resolver role override ignores exhaustedUntil

- Severity: Low (agent tooling; a dispatch fails at runtime instead of falling back)
- Evidence: `.opencode/scripts/resolve-model.mjs:181-196` — the `roleOverrides` branch returns the
  pinned model immediately without consulting `runtime.exhaustedUntil`; the exhaustion filter
  (lines 198-225) applies only to the preset chain. Observed 2026-10-24: with
  `roleOverrides.planner = opencode/glm-5.3-flash-free` and the free quota exhausted, the
  `pipe/planner` dispatch for `follow-windows-theme-changes` died with "Free usage limit reached",
  and after `--exhaust` the resolver kept returning the same model.
- Proposed remedy: apply the same `isStillExhausted` skip to the role override (fall through to the
  preset chain when the pinned model is exhausted), or at least print a loud warning on resolution.
- Trigger: any dispatch that fails on an exhausted overridden model, or the next
  `resolve-model.mjs` edit.

### Settings-page registration lives in two places

- Severity: Low (maintenance ergonomics)
- Evidence: adding a page still touches the `SettingsPage` metadata in
  `SettingsViewModel.AllPages` and the matching `views:Settings*Page` element in the
  `SettingsWindow.xaml` page host; `SettingsPageMetadataTests` can only report that the two sides
  disagree after both have been edited.
- Proposed remedy: one registration point, for example a page factory carried on the metadata.
  Deliberately not done now because it moves view creation into the view model.
- Trigger: a fifth page group or a fifteenth page.

### Single-use FontSize and CornerRadius literals stay untokenized

- Severity: Low (consistency)
- Evidence: after the sizing sweep, the 9, 10, 13, and 15 pt glyph and text tiers and the 9, 13, 7,
  and 0 px radii remain literals because each has one consumer and encodes no shared role.
- Proposed remedy: promote a value to a `Sizing.*` token when a second consumer appears; leave a
  single-use literal alone until then.
- Trigger: copy-pasting a view and reaching for the same literal.
