# Backlog

Parked findings and deferred work. Each item records evidence, severity, a proposed remedy, and the
trigger that should bring it back. The pipeline protocol (`pipeline-protocol` skill) reviews this
file during `/pipe-resume`; do not re-derive parked work from conversation history.

## Agent and process domain

### Flaky ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus

- Severity: Low (test reliability only; pre-existing, unrelated to fix-settings-open-delay)
- Evidence: failed 1 of 3 full-solution runs under parallel load during fix-settings-open-delay
  verification (2026-10-20); green in isolation and on re-run. The test races `Start()`'s background
  scan against `Stop()` — it waits only for watcher creation before stopping, so the status can still
  read `Idle` when `NotConfigured` is asserted (`tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:120-133`).
- Proposed remedy: wait for the scan to settle (idle/not-configured transition) before asserting, or
  expose a deterministic completion signal from `ProjectScanningService.Stop`.
- Trigger: next red CI/dev run of this test, or any work touching project scanning.

### Async blocked-state population on the agents page (design D2 of fix-settings-open-delay)

- Severity: Medium (perceived-latency judgment call)
- Evidence: `AgentManagementViewModel.Load` still runs the running-agent enumeration synchronously
  on the UI thread; after `fix-settings-open-delay` the pass is cheap (one snapshot plus the
  candidate-name pre-filter) but not free, and any future cost added to that path lands directly
  on Settings open.
- Proposed remedy: build cards with `CanUpdate = declaresUpdate` and populate the blocked state
  through a fire-and-forget pass when the enumeration completes — the `PopulateVersions` shape
  with a placeholder treatment for the Update action. Requires a **MODIFIED `agent-ui` spec delta
  with preserved scenario names**: "Update blocked by a running agent" is state-based, so the
  transient where a card still shows Update available while its agent runs needs explicit
  blessing; `SettingsWindow.ShowSettings` (load-before-show) would need restructuring too.
- Trigger: Settings open still perceived slow after the pre-filter, or a request for an async
  Settings load.
- Decision recorded in `openspec/changes/archive/2026-10-20-fix-settings-open-delay/design.md` (D2, rejected there).

### Monitor-level running-agent cache

- Severity: Low (performance polish with a safety risk if done naively)
- Evidence: `AgentProcessMonitor.GetRunningAgents` enumerates on every call — once per Settings
  load, on every `CommandManager` requery of the launch window Update command
  (`WindowActionCoordinator.CanExecuteUpdate`), and in the execution-time gate
  (`AgentManagementService.UpdateAsync`). After the candidate-name pre-filter each pass is one
  snapshot plus a handful of candidate reads, so the residual cost is low.
- Proposed remedy: only with an explicit invalidation story: a TTL cache would make the
  execution-time safety gate stale (an agent started inside the TTL would not block an update),
  which the `agent-ui`/`preferences-ui` blocked-state scenarios do not permit.
- Trigger: profiling shows the filtered enumeration as a hot spot on a requery path.

### Runtime-hosted agent blocking (Option B)

- Severity: Medium (product gap)
- Evidence: `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs` matches executable file
  names only; `WindowsAgentProcessInspector` does not read command lines. npm-shim agents running as
  `node.exe` are not matched, so update blocking does not fire for them.
- Proposed remedy: command-line retrieval (WMI `Win32_Process` with a timeout, or PEB) behind the
  inspector port, keeping the synchronous UI path off the blocking call.
- Trigger: a user reports an update not being blocked for an agent launched through node or python.
- Decision recorded in
  `openspec/changes/archive/2026-10-20-harden-probe-caching-and-process-matching/design.md`.

### Version population loop consolidation

- Severity: Medium
- Evidence: three loops — `AgentPaneController.PopulateVersionsAsync` (generation + CTS),
  `AgentManagementViewModel.PopulateVersions` (serial, no cancellation), and legacy
  `MainWindow.xaml.cs` (dead window). Two of them swallow exceptions into `Debug.WriteLine`.
- Proposed remedy: give the management loop the pane's CTS/generation shape; route failures to
  `ILogger`; leave the legacy window untouched (documented exception).
- Trigger: touching agent version population or a stale-version report.

### Magic version sentinels

- Severity: Low
- Evidence: `"…"` and `"unknown"` are checked in `AgentCardItem`, `AgentManagementViewModel`,
  `AgentPaneController`, and legacy `MainWindow`.
- Proposed remedy: a small `AgentVersionText` helper with `IsKnown`/`Placeholder`.
- Trigger: adding another agent surface or touching version display.

### Runtime executable PATH-scan identity (Git Bash and PowerShell 7)

- Severity: Low (design-accepted)
- Evidence: the PATH steps of `WindowsRuntimeLocator.LocateGitBash`
  (`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:233-303`) and `LocatePowerShell7` (`:304-369`)
  accept the first matching executable outside the registry/well-known steps. For bash this may be a
  non-Git-for-Windows flavor (Cygwin/MSYS); for pwsh it is deliberate, because Microsoft Store
  installs surface as a WindowsApps execution alias. The `agent-commands` requirement wording
  ("never an arbitrary `bash.exe` resolved from the operating system path") is stronger than what the
  approved lookup order guarantees.
- Proposed remedy: tighten the spec wording, or validate PATH hits against the distribution (Git:
  `git.exe` sibling or version file; pwsh: keep as-is for Store alias support).
- Trigger: a user report of a wrong bash/pwsh flavor, or any edit to the `agent-commands` spec.

### Registry InstallPath precedence can shadow a valid install

- Severity: Low
- Evidence: `WindowsRuntimeEnvironment.ReadGitForWindowsInstallPath`
  (`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:77-92`) returns the first non-empty value across
  HKLM64 → HKLM32 → HKCU views, so a stale HKLM value shadows a valid HKCU/32-bit install; the class
  has no direct unit test (only the fake seam exercises the locator).
- Proposed remedy: collect every candidate and prefer one whose `bin\bash.exe` exists; add
  real-registry layout tests.
- Trigger: a report of Git Bash marked "not installed" while Git for Windows is installed.

### Runtime integration tests pass silently when the runtime is absent

- Severity: Low
- Evidence: `LaunchAsync_GitBash_ChildStartsInWorkingDirectory` and
  `LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory`
  (`tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`) `return` early —
  reported as passed, not skipped — when the runtime cannot be resolved on the machine.
- Proposed remedy: use a skip mechanism (for example `SkippableFact`) so CI reporting distinguishes
  "not run" from "passed".
- Trigger: a CI coverage review or a hidden-regression investigation.

### Update-detection test gaps: timeout path and missing-installation indicator

- Severity: Low
- Evidence: two `agent-update-detection` scenarios are code-verified but lack direct tests —
  release-request timeout degrading to `Unknown` (`AgentReleaseClient.cs:80-84`), and "no indicator
  for a missing installation" (holds by construction: uninstalled → version null → Unknown → text
  null; covered only compositionally). Recorded in
  `openspec/changes/archive/2026-10-20-agent-update-detection/` verification.
- Proposed remedy: one handler-stub test that delays past the probe timeout, and one card test
  asserting no chip for an uninstalled agent with a declared `updateCheck`.
- Trigger: touching `AgentReleaseClient`, the availability service, or the card indicator.

## Project domain

### AddProject returns the live instance

- Severity: Low
- Evidence: `ProjectService.AddProject`/`AddDiscoveredProject` return internal `Project` instances
  while read methods return `Copy()`.
- Proposed remedy: return `Copy()`; one line plus XML docs.
- Trigger: any project-domain change.

### Hand-rolled clone siblings

- Severity: Medium (silent field loss on clone)
- Evidence: `ConfigurationSnapshot.CloneProject` duplicates `Project.Copy()`;
  `AppConfigDocument.ProjectDocument.From/ToProject` and `ClonePreferences` are further manual copies.
- Proposed remedy: `CloneProject` delegates to `Project.Copy()`; add round-trip tests for the
  persistence boundary; guard comments on the remaining manual copies.
- Trigger: next persistence-boundary change or when adding a `Project`/preferences field.

### ProjectState shares live items at the persistence boundary

- Severity: Low
- Evidence: `ProjectState.CopyTo`/`ApplyTo` share `Project` references with the cached snapshot.
- Proposed remedy: copy items when crossing the boundary.
- Trigger: anything that reads projects through `ConfigurationRepository`.

### ProjectPathPolicy duplication

- Severity: Cosmetic
- Evidence: `ProjectRegistryViewModel` re-implements normalize/compare because `ProjectPathPolicy` is
  internal to Core.
- Proposed remedy: make the policy public and reuse it.
- Trigger: path handling changes.

## Specifications and docs

### `main-window-layout` names the wrong window

- Severity: Medium (documentation debt)
- Evidence: the capability describes the launch window (hairline divider, action popovers, DWM
  rounding, Escape behavior) while `MainWindow` is an unregistered legacy reference.
- Proposed remedy: rename or annotate the capability in the next spec-touching change.
- Trigger: any `main-window-layout` edit.

### `agent-ui` requirement naming style drift

- Severity: Cosmetic
- Evidence: `REQ-AGENT-CAP-002…012` naming inside an otherwise sentence-style capability.
- Proposed remedy: normalize opportunistically when the file is edited.

## DI and lifetime

### Transient IDisposable chain retained by the root provider

- Severity: Low
- Evidence: each Settings open resolves a transient IDisposable chain from the root provider, and
  MS.DI retains transients until application exit.
- Proposed remedy: resolve through a factory that disposes explicitly, if memory profiles ever show
  growth.
- Trigger: a memory-growth report.

### Matcher's unused logger

- Severity: Cosmetic
- Evidence: `AgentProcessMatcher` keeps an unused logger (deliberately left out of the amputation).
- Proposed remedy: remove during the next dead-member sweep.

### Registered-but-unused `RuntimeSelector`

- Severity: Cosmetic (dead code with one live DI registration)
- Evidence: `src/CLIHub.Core.Windows/RuntimeSelector.cs:11` is registered at
  `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:37`, but its only consumer is
  `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs`; `RuntimeType.WindowsTerminal`
  (`src/CLIHub.Core/Infrastructure/Processes/RuntimeType.cs:11`) has no other producer.
- Proposed remedy: delete the selector, its registration, and its tests; drop
  `RuntimeType.WindowsTerminal` if the settings model no longer needs it.
- Trigger: the next dead-member sweep, or the Git Bash locator landing in the same registration file
  (found while refreshing `openspec/changes/archive/2026-10-20-git-bash-resolution`; explicitly out of its scope).

## Test suite

### Single unexplained test failure (not reproduced)

- Severity: Low (watch item)
- Evidence: during pipeline verification of `git-bash-resolution` on 2026-10-20, one run of
  `CLIHub.Core.Tests` reported 1 failed / 553 passed and the failing test name was not captured.
  Five subsequent runs were fully green, as were the implementer's and independent verifier's runs.
  Likely candidates are the pre-existing timing-sensitive tests: `UpdateCheckerTests` and
  `UpdateDownloaderTests` (100 ms cancellation windows) and `ProjectScanningServiceTests`
  (stopwatch-bounded wait loop).
- Proposed remedy: on recurrence, re-run with `--logger trx`, capture the test name, and stabilize the
  offending timing window.
- Trigger: any red `dotnet test CLIHub.sln` without a matching code change.

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

### `.gitnexus/` is excluded only by the local git exclude file

- Severity: Low (repo hygiene; a fresh clone produces untracked `.gitnexus/` noise after a GitNexus run)
- Evidence: `.gitignore` ignores `.codegraph/` and `graphify-out/` but has no `.gitnexus/` entry; this working
  copy hides it only through the local, uncommitted `.git/info/exclude:7`.
- Proposed remedy: add `.gitnexus/` to `.gitignore`, or ignore only its generated index files if the
  `.gitnexus/run.cjs` runner is meant to be versioned (it is currently untracked here).
- Trigger: the next GitNexus run on a fresh clone, or any `.gitignore` edit.

## UI and theming

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
  and 0 px radii remain literals because each has one consumer and encodes no shared role; the
  legacy `Views/MainWindow.xaml` also keeps its pre-palette colours (allowlisted by
  `ThemeColorLiteralTests`).
- Proposed remedy: promote a value to a `Sizing.*` token when a second consumer appears; leave a
  single-use literal alone until then.
- Trigger: copy-pasting a view and reaching for the same literal.

### Windows personalization changes are not followed while the application runs

- Severity: Medium (the System mode goes stale after startup)
- Evidence: `PaletteSwitcher.Apply` has exactly two callers — `App.ApplyTheme`
  (`src/CLIHub/App.xaml.cs:49`, startup) and `WpfThemePreferenceTarget.SetTheme`
  (`src/CLIHub/Settings/WpfThemePreferenceTarget.cs:39`, reached from a Settings save through
  `PreferenceApplier`). The "System" theme therefore resolves `SystemTheme.IsLight()` at startup and
  at save only; nothing refreshes the palette when Windows personalization changes afterwards.
- Proposed remedy: subscribe to `SystemEvents.UserPreferenceChanged` (or watch the relevant registry
  value) and call the same switcher with the stored theme, keeping the handler on the UI thread and
  disposing the subscription with the application.
- Trigger: a user report that switching Windows to dark leaves CLIHub light until restart.

### Unversioned README screenshot capture harness

- Severity: Low (reproducibility)
- Evidence: the 0.9.9 README refresh used an ad-hoc PowerShell harness kept only in `%TEMP%`
  (`capture-final.ps1`): it seeds `%APPDATA%\CLIHub\config.json` with a throwaway workspace, opens
  Settings through UI Automation, captures with `CopyFromScreen`, crops the 2 px ring, applies the
  12 px mask, and restores the config. `PrintWindow` intermittently returned black frames on
  2026-10-23, which is why the screen-copy path was added. `docs/images/README.md` records only the
  manual attributes (150% scaling, crop, mask), not the harness.
- Proposed remedy: commit a sanitized script under `docs/images/tools/` with parameters for the mock
  workspace and output directory, an explicit backup/restore of the user configuration, a
  black-frame retry, and a guard against overwriting `%APPDATA%\CLIHub`; alternatively extend
  `docs/images/README.md` with the exact automation steps.
- Trigger: the next README screenshot refresh.

### Accent precedence across a live palette switch (resolved)

- Status: Resolved by the `accent-independent-of-theme` change (2026-10-23). Formerly parked as
  "A settings save pins the accent even when it equals the palette default".
- Resolved model: the accent is application-scoped with a single default (Sky `#4CC2FF`) and is
  independent of the active theme. The three theme palettes declare chrome and surface brushes only;
  neither `AccentBrush` nor `AccentSelectionBrush` is a palette key. `Settings/AccentApplier.cs`
  writes both keys into `Application.Resources` (never `MergedDictionaries`) for both callers —
  `App.ApplyAccent` at startup and `WpfAccentPreferenceTarget.SetAccent` on a Settings save — and
  always applies the stored (normalized) accent, the default included. The former default-skip
  branch in `App.ApplyAccent` is gone, so there is no local-entry-versus-baked-palette precedence
  left: a theme switch never changes the accent and an accent save never changes the theme.
- Guarded by: `AccentApplierTests` (`tests/CLIHub.Tests/Settings/AccentApplierTests.cs`, including
  the palette-swap independence case), `ThemePaletteParityTests` (no palette may declare an accent
  key), and `ApplicationResourceKeyTests` (the accent keys are never declared by a palette and never
  read with `StaticResource`).
- Trigger: an explicit product decision to reintroduce per-palette or "follow-palette" accents; none is
  planned, and the change's non-goals reject both.
