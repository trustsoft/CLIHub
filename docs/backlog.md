# Backlog

Parked findings and deferred work. Each item records evidence, severity, a proposed remedy, and the
trigger that should bring it back. The pipeline protocol (`pipeline-protocol` skill) reviews this
file during `/pipe-resume`; do not re-derive parked work from conversation history. Resolved entries
are pruned from this list once their evidence is no longer needed; recover them from git history.

## Time-bound

### Signature transition cleanup after 2026-12-15

- Severity: Low (dead code and stale copy; behavior is already correct from the cutoff)
- Evidence: `RequireFeedSignature = false` (`PluginFeedSigner.cs:33`) and `SignatureRequiredAfter =
  2026-12-15` (`:39`) exist only to open the transition window; the code comment at `:26-32` describes
  the follow-up as "flipping" the flag, which is inaccurate. Why now: `IsSignatureRequired` already
  returns true from the cutoff (`src/CLIHub.Core/Plugins/PluginFeedSigner.cs:77-78`), so this is
  cleanup, not a flag flip; setting `RequireFeedSignature` to true only ends the transition window
  early and changes nothing after the cutoff.
- Proposed remedy: after the cutoff, remove `RequireFeedSignature` and the explicit-flag overload
  (`:77`), the transition branch and `SignatureMissingWarning`
  (`src/CLIHub.Core/Plugins/PluginFeedClient.cs:29`), `PluginFeedResult.SignatureWarning`
  (`PluginFeedResult.cs:40`) and its consumer (`PluginFeedSyncService.cs:278`); correct the stale
  "flip" framing in `docs/architecture/plugin-index-signing-repo.md:137`; then remove the pre-cutoff
  tests (`PluginFeedSignerTests.cs:153-163`, the before-cutoff `PluginFeedClientTests` cases,
  `PluginFeedSyncServiceTests.cs:314,417,435,459`). ADR-0015 keeps its history; record completion in
  the architecture doc, not by editing the ADR.
- Trigger: 2026-12-15 UTC — the first change after the cutoff.

## Architecture and design

### Architecture proposals (2026-10-29 analysis) — status

All six proposals have an outcome; the history lives in the ADRs and archived changes, not here.
Proposals 1–4 shipped (ADR-0019 for logging, ADR-0013 for the rejected persistence/cache halves,
ADR-0017 domain events, ADR-0018 capability split, ADR-0012 process domain); Proposal 5 was audited
on 2026-11-14 and rejected (ADR-0016, with its three real gaps parked under "Plugins"); Proposal 6
delivered its first slice, with follow-ups below.

- Trigger: Q1 2027 planning cycle, onboarding new contributors, or next major refactoring that would
  benefit from architectural clarity.

### Configuration decomposition (Proposal 6) — follow-ups

- Severity: Low (structural, no user impact expected)
- Evidence: analysis of 2026-10-29 flagged the single `AppConfig` as coupling unrelated concerns
  (preferences, projects, scanning) while ADR-0002 pins one camelCase document with one atomic write.
  The document layer already splits sections (`PreferencesDocument`, `ProjectStateDocument`, scanning
  settings), so part of the intent may already be satisfied — the slice must prove real coupling
  before restructuring anything.
- Proposed remedy: `config-decomposition-slice` change — internal domain-aligned decomposition at the
  snapshot/repository boundary only; external file format and atomic write stay untouched.
  Reject or shrink if reconnaissance shows the coupling is already contained. **Result (2026-10-29,
  commit `de590e4`, archived as `2026-10-29-config-decomposition-slice`): `ScanningPreferences` value
  object extracted (five flat `AppPreferences` properties composed as `Scanning`), document mapping
  delegates the section but keeps the identical flat camelCase wire fields, snapshot copies via
  `Scanning.Copy()`; census/round-trip/deep-copy pins in place; 1,249 tests. Deferred follow-ups:
  Agent/Update/Appearance extraction and store narrowing, each with an evidence trigger.**
- Trigger (reopen): a further cross-concern edit that still forces synchronized mapping changes, or
  the `config-format-v2` decision recorded as an ADR-0013 reopen trigger.

### Enum mappers silently swallow undefined enum values in `ToToken` (project-wide idiom)

**Status: Resolved by strict-enum-token-mappers.**

- Severity: Low (latent; every mapper coerces an undefined value to its default token)
- Evidence: review on 2026-11-04 — `RuntimeKinds.ToToken`
  (`src/CLIHub.Core/Models/Processes/RuntimeKind.cs:100-107`) maps an undefined enum value such as
  `(RuntimeKind)42` to `"ps"`, which then persists and round-trips as a valid PowerShell token. The
  same discard-arm idiom exists in `AppTheme.ToToken`, `PopupScreenPosition.ToToken`, and
  `PathDisplayStyle.ToToken`, so this is a project-wide pattern, not a per-file defect; changing one
  mapper alone would break consistency.
- Proposed remedy: decide once project-wide — keep the idiom and document it, or make all mappers
  throw on undefined values to surface bugs earlier.
- Trigger: the next new enum mapper, or a config-corruption incident where a bogus enum value got
  persisted as a valid token.

### `AgentManagementViewModel` still composes several unrelated concerns

- Severity: Medium (maintainability; the file keeps accumulating unrelated concerns)
- Evidence: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` is 62.9 KB / 1472 lines and
  composes plugin install and registry state, detection passes, update availability and per-card
  update checks, card ordering, default-agent options, and summary surfaces. Parts are already
  extracted (`IAgentProbeService`, `IAgentUpdateService`, `IAgentUpdateAvailabilityService`,
  `AgentProcessMonitor`, `AgentCardItem`); the remaining block is UI-facing orchestration.
- Proposed remedy: move multi-service workflows into Core by capability (per ADR-0018, a split needs
  demonstrated partial-use evidence); the view model keeps state, commands, and UI coordination.
  Explicitly not a split behind `IAgentManagementSection` — that is the section facade consumed by
  `SettingsViewModel`, not a decomposition mechanism.
- Re-checked (2026-11-16, recon before a planned decomposition): the file is 64,378 bytes / 1472
  lines with an 18-dependency constructor. Concern inventory by line range: catalog/card collections
  and origin summary (`:174-228`, `:510-617`), plugin install and registry workflows (`:311-352`,
  `:1273-1433`), feed badge refresh (`:619-728`), detection passes (`:958-1080`, `:1210-1231`),
  version population (`:833-899`), blocked state (`:901-956`), update availability and per-card
  checks (`:457-508`, `:1082-1252`), card ordering (`:730-776`), default-agent options (`:778-785`),
  summary surfaces (`:193-236`, `:787-798`), busy state and command gating (`:1438-1471`), and edit
  config (`:1254-1271`). Roughly two-thirds is UI state and coordination; the domain policy already
  lives in Core or in the extracted pure helpers.
- Re-checked outcome: no remaining concern meets ADR-0018's partial-use bar. Every capability that
  has a second production consumer is already extracted (`IAgentProbeService`, `IAgentUpdateService`,
  `IAgentUpdateAvailabilityService`, `AgentProcessMonitor`, `AgentCardOrdering`). The closest
  candidate — the plugin install/disable/enable workflow, whose services `BrowsePluginsDialogViewModel`
  also uses — would need a new abstraction unifying two different flows, not a split backed by an
  existing partial-use consumer. A slice would also need callbacks: every pass writes `Cards` and then
  calls `RefreshSummarySurfaces()` and `RebuildDefaultAgentOptions()`, and `CardActionScope` shares one
  busy counter across all card actions. The test surface is ~3,300 lines across 10 partial files plus
  the 355-line builder, and any constructor change touches all of them.
- Trigger (refined): a second consumer of a specific workflow (for example a feed-install path reusing
  the local `InstallPlugin` flow), or a feature that adds a concern to this view model. A size-based
  split on its own remains rejected by ADR-0018.

### Settings resource dictionaries are one file per family for every settings page

- Severity: Low (maintenance ergonomics; XAML churn)
- Evidence: `src/CLIHub/Themes/Common/SettingsControls.xaml` is 39.7 KB / 665 lines and
  `SettingsStyles.xaml` is 32.4 KB / 532 lines; both are single merged dictionaries serving all
  settings pages.
- Proposed remedy: split into multiple `ResourceDictionary` files merged by control category
  (buttons, inputs, cards, headers), not by page — the styles are shared across pages, so a per-page
  split would duplicate definitions. No behavioral change; verify each page still resolves its
  resources after the split.
- Trigger: the next addition that pushes a dictionary past roughly 700 lines, or a fifth settings
  page group.

### `PluginFeedClient` stays whole until a second consumer appears

- Severity: Low (the class is cohesive today; size alone is not a defect)
- Evidence: `src/CLIHub.Core/Plugins/PluginFeedClient.cs` is 61.5 KB / 1313 lines and covers HTTPS
  transport with redirect handling, ECDSA signature verification, index/descriptor parsing, index
  caching, and error mapping; the behavior is pinned by the `plugin-feed` and `plugin-index-signing`
  specs plus `PluginFeedClientTests` (61.7 KB / 1576 lines).
- Proposed remedy: extract signature verification, parsing, and cache/transport only when a concrete
  second consumer or a second transport appears (for example a non-index plugin source); do not split
  for size alone.
- Trigger: a second feed transport or plugin source, or a feature that needs one of those concerns
  independently.

## Plugins

### Prune semantics for registry entries whose plugin folder is gone

- Severity: Low (deliberate behavior, recorded decision; only becomes a question with a delete flow)
- Evidence (2026-11-14, `plugin-registry-consolidation` design D5/OQ-2): catalog reconciliation
  keeps registry entries whose plugin folder disappeared and logs them Information per scan
  (`src/CLIHub.Core/Plugins/PluginCatalog.cs:198`); nothing prunes them, because the seeder's
  re-seed and disabled-preservation semantics depend on entries surviving missing folders. A
  gone-folder entry still shows "Installed (current)" in Browse Plugins (accepted for this slice,
  design OQ-3).
- Proposed remedy: when a user-driven plugin delete flow lands (none exists today — disable is the
  only "removal"), have that flow prune the registry entry explicitly at delete time and decide
  whether Browse statuses should consult folder presence; keep scan-time reconciliation
  keep-only so transient folder absence (cloud-sync hiccup, antivirus quarantine) never destroys
  origin/disabled state irreversibly.
- Trigger: a future user-driven plugin delete flow (the first flow that deletes plugin folders
  through CLIHub).

### Built-in repair never runs after startup

**Status: Resolved by seed-builtins-on-manual-reload.**

- Resolution: "Reload Catalog" now calls `IPluginSeeder.EnsureBuiltinsExist()` before
  `IPluginCatalog.ReloadPlugins()`, so a deleted built-in descriptor is restored by the manual
  reload instead of waiting for a restart; automatic reloads stay seed-free.
- Severity: Low (self-heals on restart; no data loss)
- Evidence: `EnsureBuiltinsExist` is called only from
  `src/CLIHub/Startup/PluginInitializationService.cs:27`, and `Initialize` runs once from
  `StartupStateLoader` (`src/CLIHub/Startup/StartupStateLoader.cs:32`). The manual reload path,
  `PluginCatalog.ReloadPlugins` (`src/CLIHub.Core/Plugins/PluginCatalog.cs:71-76`), only rescans the
  plugins folder. Delete a built-in descriptor, run "Reload Catalog", and the built-in stays absent
  until the next restart.
- Proposed remedy: seed on the manual reload path (needs a spec delta on reload semantics) or
  document the limitation as intended behavior.
- Trigger: a user report of a missing built-in after reload, or the next change to the reload
  contract.

### Locally modified plugins are reported but never consulted

**Status: Resolved by plugin-state-integrity (Part B — documented as accepted behavior).**

- Severity: Medium-Low (silent loss of a user's hand edit on the next feed update)
- Evidence: `PluginCatalog.VerifyContentHash` records user-modified entries
  (`src/CLIHub.Core/Plugins/PluginCatalog.cs:226-246`) and `GetUserModifiedPlugins` exposes them
  (`:258`), but no consumer under `src/` reads the getter; the update path derives replace-consent
  only from feed status
  (`src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs:214-230,314-318,406-410`), and
  `RecordInstalledState` rewrites the content hash unconditionally
  (`src/CLIHub.Core/Plugins/PluginInstaller.cs:501-513`), so the modification stops being detectable.
- Proposed remedy: surface a "locally modified" flag in `PluginInstallPreview` and require explicit
  confirmation before overwriting a modified descriptor.
- Trigger: the next change to the install/update confirmation flow, or a user report of a lost edit.

### Registry save failures are invisible

**Status: Resolved by plugin-state-integrity (Part A — TrySave returns bool, callers surface failure).**

- Severity: Low-Medium (the UI reports a state change that did not persist)
- Evidence: `IPluginRegistry.Save` returns `void` (`src/CLIHub.Core/Plugins/IPluginRegistry.cs:21`)
  and swallows write failures with a log line
  (`src/CLIHub.Core/Plugins/PluginRegistryRepository.cs:97-105`). Disable and enable
  (`AgentManagementViewModel.cs:1320-1326`, `:1339-1345`) reload the catalog straight after the
  save, so a failed write leaves the previous state in effect while the surrounding `catch` cannot
  fire — the user sees only a log entry.
- Proposed remedy: return a success flag and surface "could not save the plugin state" in the UI;
  use it for the persist-then-reload idiom repeated in the installer, disable, and enable paths.
- Trigger: a report of a plugin re-enabling itself after restart, or the next change to registry
  persistence.

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
- Evidence: `src/CLIHub/Views/LaunchWindow.xaml.cs:42` calls `InitializeComponent()`, and the XAML root
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

### CI enforces neither the warning baseline nor coverage

**Status: Resolved by ci-quality-gates.**

- Resolution: the CI `Build` step now runs `dotnet build CLIHub.sln -c Release -warnaserror`, and each
  `dotnet test` invocation collects `XPlat Code Coverage` into separate `artifacts/coverage/<project>/`
  directories published as one `coverage-results` artifact with no threshold — both halves of the remedy
  below, implemented in `.github/workflows/ci.yml` (`ci-quality-gates`, 2026-11-16). Release-pipeline
  parity is tracked as its own entry in this section.
- Severity: Low (hardening; the current discipline is social, not enforced by CI)
- Evidence: `Directory.Build.props` sets `EnforceCodeStyleInBuild` but not `TreatWarningsAsErrors`;
  `.editorconfig` keeps IDE0009, IDE0011, IDE0065, IDE0161, and IDE1006 at `warning`; the CI workflow
  (`.github/workflows/ci.yml`) runs plain `dotnet build`/`dotnet test` without `-warnaserror`; and
  `coverlet.collector` is referenced by all three test projects, yet no workflow collects or publishes
  coverage.
- Proposed remedy: decide per item — (a) run the CI build with `-warnaserror`, or narrow
  `WarningsAsErrors` to the categories the repository already keeps at zero, so a future SDK analyzer
  warning does not become build-breaking unintentionally; (b) collect and publish coverage as an
  artifact/trend without a hard threshold (an 80% gate is rejected as arbitrary for a WPF codebase).
- Trigger: the next CI or toolchain change, or release preparation.

### Release pipeline does not enforce warnings or coverage

- Severity: Low (the CI gates now run on every push to `master`, so a tag is cut from an already-gated
  commit; the gap only matters for a tag pushed from an ungated branch)
- Evidence: `.github/workflows/release.yml:29` runs `dotnet build CLIHub.sln -c Release` without
  `-warnaserror`, and `.github/workflows/release.yml:32` runs
  `dotnet test CLIHub.sln -c Release --no-build` without `--collect`, so the release `test` job neither
  fails on a new warning nor produces coverage. `ci-quality-gates` (2026-11-16) added both gates to
  `.github/workflows/ci.yml` and deliberately left the release pipeline untouched.
- Proposed remedy: mirror the CI gates in the release pipeline's `test` job — add `-warnaserror` to the
  `Build` step and `--collect:"XPlat Code Coverage" --results-directory artifacts/coverage/<project>`
  (or one solution-wide collection, since that job already runs `dotnet test CLIHub.sln`) plus the
  `coverage-results` upload to the `Test` step; then update the `release-pipeline` spec delta.
- Trigger: the next release-pipeline change or release-preparation change that touches
  `.github/workflows/release.yml`.

## Agent tooling

### Two sessions in one working tree can revert each other's OpenSpec files

- Severity: Medium (silent loss of archived state; no error surfaces)
- Evidence: a second session in the same checkout reverted another session's archived change, an
  applied spec scenario, and its pipeline state. Worktree isolation is now a repo rule in `AGENTS.md`,
  and the mitigation shipped in `concurrent-session-archive-safety` (2026-11-02).
- Proposed remedy: keep each session in its own worktree and re-check `git status` plus
  `openspec list` after every `openspec archive` instead of trusting the command's own output.
- Recovery: restore tracked files from the last good commit and reconstruct `.opencode/pipeline.json`;
  the steps live in
  `openspec/changes/archive/2026-11-02-concurrent-session-archive-safety/design.md`.
- Trigger: any second session in the same checkout, or an archived change reappearing in
  `openspec list`.

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

### `pipeline-state check` cannot validate commit shas from foreign repositories

- Severity: Low (informational warning only; `check` still exits 0 and the sidebar is unaffected)
- Evidence: `.opencode/scripts/pipeline-state.mjs:763-795` validates every recorded sha with
  `git cat-file -e` (`:789`) against a single repo root (`--repo`, defaulting to the state file's
  parent — the CLIHub repo). The `opencode-sidebar-suite-packaging` entry records `0185674` from
  `D:\Projects\opencode-sidebar-suite`, so `check` reports a standing `sha-missing` warning (first
  seen 2026-10-24). `update` cannot clear a sha (empty values are rejected at
  `.opencode/scripts/pipeline-state.mjs:421-423`), and a direct JSON edit would bypass the
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

### Local LM Studio models carry no reasoning-effort variant

- Severity: Low (dispatch works; only effort control is missing for the local preset)
- Evidence (2026-11-06): OpenCode's LM Studio discovery reports no `variants` for the five
  `lmstudio/*` models (`opencode api get /api/model` lists them with an empty `variants` array),
  although LM Studio itself exposes reasoning as `capabilities.reasoning.allowed_options`
  `["off","on"]` in `GET /api/v1/models`. With `variants: []` in
  `docs/agent-model-policy.json`, the resolver always emits a bare id, so a `local_first` dispatch
  cannot pin thinking off (cheaper/faster) or on (deeper) the way the hosted chains do.
- Proposed remedy: map the LM Studio reasoning switch onto the OpenCode variant vocabulary once the
  provider package exposes it (likely `none`/`high`); until then the policy keeps `variants: []` and
  the limitation is documented in the catalogue note and `docs/agent-model-policy.md`.
- Trigger: a provider-package release that surfaces LM Studio reasoning as variants, or the first
  local dispatch that needs thinking pinned off.

## UI and theming

### Settings-page registration lives in two places

- Severity: Low (maintenance ergonomics)
- Evidence: adding a page still touches the `SettingsPage` metadata in `SettingsPageCatalog.AllPages`
  (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:39`) and the matching `views:Settings*Page`
  element in the `SettingsWindow.xaml` page host; `SettingsPageMetadataTests` can only report that the
  two sides disagree after both have been edited.
- Proposed remedy: one registration point, for example a page factory carried on the metadata.
  Deliberately not done now because it moves view creation into the view model.
- Trigger: a fifth page group or a fifteenth page.
- Re-checked (2026-11-02): `SettingsPageCatalog.AllPages` now has 10 pages in 3 groups (the
  Agent Detection page was added after the previous recheck), so neither threshold is close;
  remains deferred.

### AvailabilitySummary is declared but unbound

**Status: Resolved by remove-availability-summary.**

- Severity: Low (dead surface; the property and its tests have no consumer)
- Evidence: `IAgentManagementSection.AvailabilitySummary`
  (`src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:48`) is declared on the interface with
  zero XAML bindings; the AGENTS summary card switched to `PluginCatalogSummary`. The gap that
  motivated this entry — plugin-management members reachable only through reflection — is resolved
  (`InvalidCards`, `DisabledCards`, `HasDisabledPlugins`, `PluginCatalogSummary`,
  `EnablePluginCommand` are on the interface).
- Proposed remedy: decide whether `AvailabilitySummary` is still needed; if not, remove it with its
  tests (`AgentManagementViewModelTests.Detection.cs`, `.Load.cs`); if yes, bind it.
- Trigger: the next change touching `IAgentManagementSection`, the CLI Agents page, or any non-XAML
  consumer of `AgentsState`.

### Single-use FontSize and CornerRadius literals stay untokenized

- Severity: Low (consistency)
- Evidence: after the sizing sweep, the 9, 10, 13, and 15 pt glyph and text tiers and the 9, 13, 7,
  and 0 px radii remain literals because each has one consumer and encodes no shared role.
- Proposed remedy: promote a value to a `Sizing.*` token when a second consumer appears; leave a
  single-use literal alone until then.
- Trigger: copy-pasting a view and reaching for the same literal.

### Plugin Actions popup has no explicit close on page collapse

- Severity: Low (the popup closes on outside click and window teardown; only a synthetic collapse
  while it is open could leave it visible)
- Evidence (2026-11-05): `SettingsAgentsPage.xaml` hosts `PluginActionsPopup` with
  `StaysOpen="False"`; `SettingsAgentsPage.xaml.cs` closes it on entry click and Escape but not on
  `Unloaded` or `IsVisibleChanged`. The Settings host toggles pages by visibility, so navigation is
  normally an outside click that already closes the popup; the Launch window's menus have the same
  shape.
- Proposed remedy: close `PluginActionsPopup` from an `Unloaded`/`IsVisibleChanged` handler if a
  non-click page transition can collapse the page while the menu is open.
- Trigger: a report of a stale popup after navigation, or a change to the Settings page host.

### Settings card CanUpdate does not itself test installation state

- Severity: Low (no reachable wrong state today; the not-installed gate relies on every caller clearing availability)
- Evidence (2026-11-09): `AgentCardItem.CanUpdate` derives from the availability status, the declared
  `update` command, and `IsBlockedByRunningAgent` only (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:209-212`).
  A not-installed card is disabled because every production path skips or clears availability
  (`AgentManagementViewModel` load and check paths); a direct assignment of `UpdateAvailable` to a
  not-installed card would compute `CanUpdate == true`.
- Proposed remedy: add `this.IsInstalled` to the derivation and raise `CanUpdate` from the
  `InstallState` setter, or add a card-level test that pins the current caller-owns-the-gate contract.
- Trigger: a new availability writer that can leave a result behind an uninstall, or the next change
  touching `AgentCardItem.CanUpdate`.
