# Backlog

Parked findings and deferred work. Each item records evidence, severity, a proposed remedy, and the
trigger that should bring it back. The pipeline protocol (`pipeline-protocol` skill) reviews this
file during `/pipe-resume`; do not re-derive parked work from conversation history. Resolved entries
are pruned from this list once their evidence is no longer needed; recover them from git history.

## Architecture and design

### Architectural improvement proposals (2026-10-29 analysis)

- Severity: Medium (strategic, not urgent; affects long-term maintainability and evolution)
- Evidence: comprehensive architectural analysis completed 2026-10-29 (commit `e141732`); the
  analysis documents were since removed, so the full text lives in git history at that commit. The
  analysis identified 6 concrete improvement proposals:
  (1) Extract cross-cutting services (logging, persistence, caching); (2) Introduce domain events;
  (3) Capability-based service decomposition; **(4) Process domain model unification [IMPLEMENTED 2026-10-29]**;
  (5) Plugin lifecycle management; (6) Configuration decomposition (internal). Health score: 8.5/10. Key
  concerns: coordinator proliferation (5 coordinators), large role-based service interfaces,
  UI-heavy orchestration, configuration coupling.
- Proposed remedy: Three-phase evolution roadmap (Q1-Q3 2027): Phase 1 (foundation) — cross-cutting
  services and domain events; Phase 2 (domain refinement) — service decomposition, **process domain [ALL PHASES COMPLETE 2026-10-29]**,
  plugin lifecycle; Phase 3 (configuration evolution) — internal decomposition. Immediate actions
  (next 30 days): document architecture principles, extract `ILoggingContextProvider`, implement
  first domain event POC (`ProjectSelected`). See the 2026-10-29 analysis in git history at
  `e141732` for full navigation. **Proposal 4 complete: Phase 1+2 `a64422c`, Phase 3 `37e3c46`, Phase 4 `f7c1495` (obsolete
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

### Configuration decomposition (Proposal 6) - first slice done, follow-ups deferred

- Severity: Low (structural, no user impact expected)
- Evidence: analysis of 2026-10-29 flagged the single `AppConfig` as coupling unrelated concerns
  (preferences, projects, scanning) while ADR-0002 pins one camelCase document with one atomic write.
  The document layer already splits sections (`PreferencesDocument`, `ProjectStateDocument`, scanning
  settings), so part of the intent may already be satisfied - the slice must prove real coupling
  before restructuring anything.
- Proposed remedy: `config-decomposition-slice` change - internal domain-aligned decomposition at the
  snapshot/repository boundary only; external file format and atomic write stay untouched.
  Reject or shrink if reconnaissance shows the coupling is already contained. **Result (2026-10-29,
  commit `de590e4`, archived as `2026-10-29-config-decomposition-slice`): `ScanningPreferences` value
  object extracted (five flat `AppPreferences` properties composed as `Scanning`), document mapping
  delegates the section but keeps the identical flat camelCase wire fields, snapshot copies via
  `Scanning.Copy()`; census/round-trip/deep-copy pins in place; 1,249 tests. Deferred follow-ups:
  Agent/Update/Appearance extraction and store narrowing, each with an evidence trigger.**
- Trigger (reopen): a further cross-concern edit that still forces synchronized mapping changes, or
  the `config-format-v2` decision below.

### Enum mappers silently swallow undefined enum values in `ToToken` (project-wide idiom)

- Severity: Low (latent; every mapper coerces an undefined value to its default token)
- Evidence: review on 2026-11-04 — `RuntimeKinds.ToToken`
  (`src/CLIHub.Core/Models/Processes/RuntimeKind.cs:110-116`) maps an undefined enum value such as
  `(RuntimeKind)42` to `"ps"`, which then persists and round-trips as a valid PowerShell token. The
  same discard-arm idiom exists in `AppTheme.ToToken`, `PopupScreenPosition.ToToken`, and
  `PathDisplayStyle.ToToken`, so this is a project-wide pattern, not a per-file defect; changing one
  mapper alone would break consistency.
- Proposed remedy: decide once project-wide — keep the idiom and document it, or make all mappers
  throw on undefined values to surface bugs earlier.
- Trigger: the next new enum mapper, or a config-corruption incident where a bogus enum value got
  persisted as a valid token.

## DI and lifetime

### Transient IDisposable chain retained by the root provider

- Severity: Low
- Evidence: each Settings open resolves a transient IDisposable chain from the root provider, and
  MS.DI retains transients until application exit.
- Proposed remedy: resolve through a factory that disposes explicitly, if memory profiles ever show
  growth.
- Trigger: a memory-growth report.

## Test suite

### `LaunchWindow` cannot be constructed outside an `Application`

- Severity: Low (test-only; forces workarounds, no product impact)
- Evidence: `src/CLIHub/Views/LaunchWindow.xaml.cs:44` calls `InitializeComponent()`, and the XAML root
  resolves `StaticResource Sizing.FontFamily.Text` from application-level merged dictionaries, so
  constructing the window in the test host throws `XamlParseException`
  (`tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs:12-20` records the limitation). Two changes
  have now paid for it: that guard test settles for a plain `Window`, and
  `remove-startup-window-marker` returns `(LaunchWindow)null!` from a strict mock because no real
  instance can exist.
- Proposed remedy: move the resource-dependent lookup out of the constructor path (for example apply the
  font through a style that resolves after load), or give the test host a minimal `Application` with the
  theme dictionaries merged, so a real window can be built in tests.
- Trigger: a third test that needs a live `LaunchWindow`, or a window-level behavior that the view model
  cannot cover.

### `Skip.If` plus `return;` depends on the method not being `[DoesNotReturn]`

- Severity: Low (latent; only a package upgrade can trigger it)
- Evidence: `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` calls
  `Skip.If(true, "...")` and then keeps a `return;` inside each `if (resolved… is null)` block. The
  `return;` is unreachable at runtime but is what makes the nullable flow analysis treat the value as
  non-null afterwards, so no null-forgiving operator is needed. `Skip.If` is not annotated
  `[DoesNotReturn]` in `Xunit.SkippableFact` 1.5.85; if a future version adds that annotation, both
  `return;` statements become `CS0162` and the repository's zero-warning build gate fails.
- Proposed remedy: on the next `Xunit.SkippableFact` upgrade, remove the two `return;` statements and
  confirm the build still reports 0 warnings and no `CS8604`/`CS8602` at the use sites; if narrowing is
  lost, restore an explicit guard instead of suppressing the warning.
- Trigger: any version bump of `Xunit.SkippableFact`.

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
- Re-checked (2026-10-31, HEAD `6f898f0`): a full `dotnet test CLIHub.sln` run was green
  (550 + 63 + 692 = 1,305, 0 failed), so the incident did not reproduce. Stays a watch item; not closed.
- Re-checked again (2026-10-31, HEAD `ccf4285`): full suite green (550 + 63 + 692 = 1,305, 0 failed,
  TRX captured under the ignored `artifacts/testlogs/`), plus 10/10 consecutive green runs of
  `--filter "FullyQualifiedName~ProjectScanningServiceTests"`. No failure name to capture; the
  assertion was not changed. Stays a watch item.

### Speculative test-infrastructure ideas from `tests/CLIHub.Tests/README.md`

- Severity: Low (speculative; the current suite works without any of them)
- Evidence: the "Future Improvements" section formerly in `tests/CLIHub.Tests/README.md` (commit `1c27693`)
  listed five enhancements that were never planned, and the section was deleted when that file was
  trimmed to the `LaunchWindowViewModelBuilder` walkthrough (`testing-conventions-doc`, 2026-11-01).
  Five ideas are preserved here: builders for `ProjectPaneController` and `AgentPaneController`;
  AutoFixture integration for automatic test data generation; snapshot testing for complex UI state;
  BenchmarkDotNet performance benchmarks for critical paths; and cross-layer end-to-end integration
  tests. The third builder the section named, `SettingsViewModelBuilder`, is deliberately **not** carried
  forward: `tests/CLIHub.Tests/Builders/SettingsViewModelFixture.cs` already constructs
  `SettingsViewModel` with sensible mock defaults and exposes the mocks for configuration and assertions,
  so parking it as missing would misstate the current suite.
- Proposed remedy: none until a concrete need appears. If one does, add the specific builder to
  `tests/CLIHub.Tests/Builders/` first; treat AutoFixture, snapshot testing, and BenchmarkDotNet as
  separate decisions, each needing its own change and its own dependency review.
- Trigger: a contributor wants a builder, UI-state verification, or performance measurement that the
  current suite does not provide.

## Agent tooling

### Two sessions in one working tree can revert each other's OpenSpec files

- Severity: Medium (silent loss of archived state; no error surfaces)
- Evidence: on 2026-11-01 a second orchestrator session worked `openspec/changes/agent-detection-defaults-detail-copy/`
  in the same checkout. At 20:12:52, while the first session was between the archive commit of
  `watched-directory-duplicate-feedback` (20:03) and the commit of `remove-startup-window-marker`
  (20:19), that change's archived files disappeared, an older pre-tick copy of the change reappeared
  under `openspec/changes/`, and `openspec list` started reporting the already-archived change as
  active again; the same operation also removed the change's already-applied scenario from
  `openspec/specs/preferences-ui/spec.md`. Nothing in the first session's git history explains it (no
  stash, no HEAD move), so the most likely cause is the other session checking out or restoring paths.
  The damage was repaired from HEAD and verified (`git status` clean, `openspec list` correct,
  `validate` clean, 225 archived changes intact).
- Proposed remedy: give each concurrent session its own worktree (`opencode.worktree_create`) instead
  of sharing one checkout, and after every `openspec archive` re-check `git status` plus
  `openspec list` rather than trusting the command's own output.
- Status: mitigated by `concurrent-session-archive-safety` (2026-11-02): worktree isolation, post-archive
  checks, and sole-writer discipline are now rules; reopen if a shared-checkout loss recurs.
- Trigger: any second session in the same checkout, or an archived change reappearing in
  `openspec list`.

### Recovery from concurrent-session loss

When concurrent-session loss is detected (archived change reappears, spec reverts, pipeline state emptied):

**Tracked OpenSpec/spec files:**
1. Run `git reflog` to find the last known good commit before the loss.
2. Run `git restore --source=<sha> -- <paths>` to restore specific files (preferred), or
3. Run `git reset --hard <sha>` to restore the entire working tree (requires explicit user approval per
   AGENTS.md, and only when unrelated work is not at risk).

**`.opencode/pipeline.json` (gitignored):**
1. Restore from the OpenCode snapshot store (for example `~/.local/share/opencode/snapshot/<project-hash>`
   on this machine), or
2. Reconstruct entries with `node .opencode/scripts/pipeline-state.mjs add/update`.

**Verify restoration:** Run `git status`, `openspec list`, `openspec validate --all`, and
`node .opencode/scripts/pipeline-state.mjs check`.

The 2026-11-01 OpenSpec files were restored from HEAD, and the 2026-11-02 pipeline state from a session
snapshot.

### `graphify update .` success is ambiguous to shell wrappers

- Severity: Low (tooling; only matters to scripts that judge the command by wrapper status)
- Evidence: `graphify update .` finishes with `Code graph updated.`, rewrites `graphify-out/graph.json`,
  `graph.html`, and `GRAPH_REPORT.md`, and returns `$LASTEXITCODE = 0` (measured directly on 2026-11-01).
  Yet three earlier pipeline runs reported "Exited with code 1" for the same command, because graphify
  writes advisory warnings to stderr (unclassified extensions, "N code file(s) yielded no symbols",
  community relabel hints) and PowerShell surfaces them as `NativeCommandError` under the strict error
  preference the tool runs with. The exit code was never the problem; the wrapper status was.
- Proposed remedy: send those advisories to stdout, or document that the command's success is
  `$LASTEXITCODE == 0` plus the `Code graph updated.` line, so gates read the real code instead of the
  wrapper status. If neither is possible, the gate should run graphify with the strict preference
  relaxed instead of treating the noise as a failure.
- Trigger: the first gate script that fails or warns on a graphify refresh that actually succeeded.

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
- Re-checked (2026-10-31): the foreign-sha entry is no longer in `.opencode/pipeline.json`;
  `pipeline-state check` reports 10 ok / 0 warnings, so the standing symptom is gone. The check path
  still validates shas against a single repo root, so the item stays open latently until the first
  foreign-repo sha returns.

### Pipeline protocol has no documented close-out for stages that do not apply

- Severity: Medium (a finished entry stays visible as unfinished and can never be pruned)
- Evidence (2026-11-03): the `docs-actualization` entry sat at `commit`/`done` with the `archive` row
  `pending` because docs-only work has no OpenSpec change to archive; the orchestrator had no
  documented way to close it. The same class of record (`2026-11-03-launch-window-copy`, pointer
  `archive` with a frozen `pending` `commit` row) came from stage jumps in `update`. The repo script
  now backfills passed stage rows and warns via `no-archive-path` / `inconsistent-stages`
  (`.opencode/scripts/pipeline-state.mjs`), but the global `pipeline-protocol` skill still describes
  only the OpenSpec archive path.
- Proposed remedy: in `D:\Projects\opencode-sidebar-suite`, add the rule to the skill — every entry
  finishes at `archive`/`done`; a stage that does not apply is recorded `done` with a note beginning
  `n/a: <reason>`; advancing the pointer closes earlier rows. Decide whether the skill source moves
  into that repository and is deployed by `install.ps1`, which currently copies only `plugins/` and
  `shared/`.
- Trigger: the next docs-only or tooling entry that has to be closed, or the next edit of the global
  `pipeline-protocol` skill.

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
- Re-checked (2026-11-02): `SettingsPageCatalog.AllPages` now has 10 pages in 3 groups (the
  Agent Detection page was added after the previous recheck), so neither threshold is close;
  remains deferred.

### Release notes requirement says "status card" but the link is on the title row

- Severity: Low (spec accuracy; no behavior or UI defect)
- Evidence: `openspec/specs/preferences-ui/spec.md:651-659` — the "Release notes from Settings"
  requirement body says the user opens release notes "from the status card", but the
  `Release notes →` link lives in `UpdatesHeaderTitleExtra` on the page title row
  (`src/CLIHub/Views/SettingsWindow.xaml:92-113`). The mismatch predates the Updates copy revision
  and was left untouched there to keep that change label-only.
- Proposed remedy: reword the requirement body to "from the title row" in the next edit of that
  requirement or during a spec-accuracy sweep.
- Trigger: the next change touching "Release notes from Settings", or a documentation audit.

### Single-use FontSize and CornerRadius literals stay untokenized

- Severity: Low (consistency)
- Evidence: after the sizing sweep, the 9, 10, 13, and 15 pt glyph and text tiers and the 9, 13, 7,
  and 0 px radii remain literals because each has one consumer and encodes no shared role.
- Proposed remedy: promote a value to a `Sizing.*` token when a second consumer appears; leave a
  single-use literal alone until then.
- Trigger: copy-pasting a view and reaching for the same literal.

### Decorative icon glyphs are exposed to assistive technology across the windows

- Severity: Low (no interactive control is affected; the glyphs are non-focusable text)
- Evidence: every decorative icon `TextBlock` carries its private-use glyph as text, and WPF exposes an
  undecorated `TextBlock` in the automation tree with that text as its accessible name —
  `FrameworkElementAutomationPeer.GetNameCore()` falls back to `GetPlainText()`, and
  `TextBlockAutomationPeer.IsControlElementCore()` keeps it in the Content view. A probe against the
  .NET 10 reference assemblies confirmed it:
  `TextBlockAutomationPeer: Name='\uE713' CtrlType=Text IsContent=True`. Current sites: the Settings
  page-header emblem (`src/CLIHub/Views/SettingsWindow.xaml:424-430`, whose comment already records
  the WPF limitation), the sidebar page glyph (`SettingsWindow.xaml:362-367`), the `SettingCard` icon
  box (`src/CLIHub/Themes/Common/SettingsStyles.xaml:214-230`), the section-header glyph
  (`SettingsStyles.xaml:390-396`), and the status-chip glyph (`SettingsWindow.xaml:123`). The
  `settings-page-icons` change centralized the glyph values in `Theming/IconGlyphs.cs`, which makes a
  shared decorative element easier to apply, but it did not add an accessibility opt-out. There is no
  per-element opt-out: `AutomationProperties.Name=""` is ignored (the peer falls back to the text) and
  `AutomationProperties.AccessibilityView` does not exist in WPF.
- Proposed remedy: one shared decorative-glyph element whose `OnCreateAutomationPeer()` returns `null`
  (or a peer reporting `IsContentElement`/`IsControlElement` false), applied consistently wherever an
  icon duplicates adjacent text, in one pass across the windows rather than one glyph at a time.
- Trigger: an accessibility requirement or audit, or the next change that adds icons to a
  screen-reader-facing surface.
