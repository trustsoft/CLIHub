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
  subscriber activation fixed and pinned; 1,248 tests, smoke confirmed).**
  **Proposal 3 first slice complete: `capability-service-slice` `774d468` (IAgentManagementService split
  into IAgentProbeService + IAgentUpdateService, `AgentManagementViewModel` composes both; zero behavior
  change, 1,248 tests). Further decompositions deferred until consumer/test subset-use evidence appears.**
- Trigger: Q1 2027 planning cycle, onboarding new contributors, or next major refactoring that would
  benefit from architectural clarity.
- Status (2026-11-14): Proposals 3, 5 and 6 are no longer open. Proposal 3 is delivered in substance
  by `capability-service-slice` — `IUpdateService` is now a composite of five capability interfaces
  (`src/CLIHub.Core/Updates/IUpdateService.cs:6-10`), so agent management and update checking are
  consumed through the slices consumers actually use; deeper splits stay deferred until subset-use
  evidence appears. Proposal 5 was audited on 2026-11-14 and rejected — the lifecycle responsibilities
  already have owners; see ADR-0016 and the "Plugin lifecycle" section below. Proposal 6 slice 1
  delivered (`config-decomposition-slice`, above) with its follow-ups deferred.

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

### AgentReleaseClient null-forgiving suppressions removable via a TryResolve out-parameter

**Status: Resolved by `followups-quick-wins-2` — API reshaped to return validated addresses directly, suppressions removed from all call sites, test assertions extended to verify resolved address.**

- Severity: Low (nullability hygiene; behavior is correct today)
- Evidence: `src/CLIHub.Core/Agents/AgentReleaseClient.cs:119-120` still uses
  `updateCheck.Package!` / `updateCheck.Repo!`. Validation lives in
  `UpdateCheckSource.TryResolve` (`src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs`), which
  since `simplify-mechanical-sweep` routes its predicates through the attributed
  `PluginUpdateCheck.HasPackage`/`HasRepo` — but the indirection breaks Roslyn's flow analysis,
  so the suppressions cannot be removed honestly without reshaping the API.
- Proposed remedy: extend `UpdateCheckSource.TryResolve` with an `out string? address` (or
  equivalent) so callers receive the validated values directly and drop the `!` operators.
  Touches 3 production call sites and 14 test call sites in `UpdateCheckSourceTests.cs`.
- Trigger: the next change touching `AgentReleaseClient` or `UpdateCheckSource`.

### Optional spec scenario pinning the in-flight probe eviction invariant

**Status: Resolved by `spec-copy-accuracy-sweep` (2026-11-14)** — added scenario "No eviction of newer in-flight probe" to the "Concurrent version probes are coalesced" requirement in `agent-version`; the test at `ProbeCacheTests.cs:194` is the durable regression pin.

- Severity: Low (spec hardening; the behavior is implemented and regression-tested)
- Evidence: `probe-cache-extraction` fixed the delayed-`finally` race where an awaited probe's
  continuation could evict a newer probe's in-flight entry (`ProbeCache.cs:66` uses
  compare-and-remove). The `agent-version` and `agent-update-detection` specs already pin
  coalescing as SHALLs, and the race is pinned by
  `ProbeCacheTests.GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe`.
  The change's design deferred adding an explicit spec scenario for the invariant itself.
- Proposed remedy: when either spec is next touched by a delta-bearing change, add a scenario
  stating that a delayed continuation of an awaited probe cannot evict a newer probe's in-flight
  request.
- Trigger: the next MODIFIED delta to `agent-version` or `agent-update-detection`.

### DomainEventBus subscription lists are not synchronized

**Status: Resolved by `followups-quick-wins` (2026-11-14)** — added `ReaderWriterLockSlim` with read locks for publishes and write locks for subscribes/unsubscribes, with a concurrency test covering 1000+ concurrent publishes and subscribe/unsubscribe operations.

- Severity: Low (latent race; not clearly reachable in normal operation — windows are sub-microsecond and publishes are rare)
- Evidence (2026-11-14, `plugin-feed-background-sync` verification): `DomainEventBus` keeps its subscription map and per-type lists unsynchronized (`src/CLIHub.Core/Events/DomainEventBus.cs:12,59-74`); `Publish` snapshots via `ToList()` (:36). Before this change every publish ran on the UI thread (`ProjectSelected`, `AgentLaunched`); the new `PluginFeedSyncService` publishes `PluginFeedUpdatedEvent` from a background thread, so a background `Publish` racing a UI-thread `Subscribe`/`Dispose` of the same event type could throw `InvalidOperationException` from the snapshot. Normal operation is safe (the view model subscribes once at construction, disposes once at teardown; publishes happen on the first fetch and hourly on change only).
- Proposed remedy: replace the list with a copy-on-write snapshot (immutable array swap) or lock around subscribe/unsubscribe/publish-snapshot, whichever matches the bus's simplicity.
- Trigger: the first background publisher whose subscription lifetime overlaps the UI subscribe window (e.g. a per-page subscriber), or a report of a collection-modified exception from `DomainEventBus`.

## Plugin registry v2

Roadmap for the plugin registry redesign (ADR-0015: separate feed repository
`trustsoft/CLIHub-plugins-v2`, CI-generated index, parallel-feed migration). Slices are ordered;
each is a separate OpenSpec change except the repo bootstrap, which is out-of-repo infrastructure.

### Bootstrap the CLIHub-plugins-v2 repository

- Severity: Medium (blocks every other registry v2 slice; the client default cannot point at a
  non-existent feed)
- Evidence: ADR-0015 (2026-11-14) fixes the v2 design: per-plugin `plugins/<id>/plugin.json`
  folders as the source of truth, a GitHub Actions workflow that validates descriptors and
  generates `index.json` (schemaVersion 2) on merge to `master`, branch protection on `master`.
- Proposed remedy: create `trustsoft/CLIHub-plugins-v2` with the layout, the
  validation/generation workflow, the v2 entry schema (inline `descriptor`, `compatibility`
  range, `channel`, null `signature`), and migrate the current v1 entries as the seed set;
  reserve `index.sig`.
- Trigger: the decision to start registry v2 implementation; must complete before the client
  default switch (next slice).
- **Slice complete (2026-11-14): `trustsoft/CLIHub-plugins-v2` created on `master` with the
  descriptor-folder layout, CI validation/generation workflow, seed migration of the 7 v1 plugins
  (sha256 verified), branch protection with the required `generate-index` check, and the
  auto-PR/`workflow_dispatch` regeneration flow. Signing workflow added by the index-signing
  slice.**

### Client v2 feed support: dual-format parser, default switch, descriptor-only install

- Severity: Medium (the first CLIHub-side slice of ADR-0015)
- Evidence: ADR-0015; `PluginFeedClient.ParseFeedIndex` accepts only `schemaVersion` 1;
  `FeedUrlProvider.DefaultFeedUrl` points at the frozen v1 index; descriptor-only plugins
  currently require a zip package although most are a single JSON descriptor plus an optional
  logo.
- Proposed remedy: one OpenSpec change — parse `schemaVersion` 2 entries (inline descriptor;
  zip entries stay checksum-verified as today), switch `DefaultFeedUrl` to the v2 index,
  add the descriptor-only install fast path (write descriptor files directly, logo through the
  logo cache, no per-package checksum step), and a spec delta on `plugin-feed`.
- Trigger: the v2 repository bootstrap is complete and seeded.
- **Slice complete (2026-11-14, `plugin-feed-v2-client`): dual-format parsing (schema 1 and 2),
  v2 `DefaultFeedUrl`, inline-descriptor validation, `compatibility` range gating
  (`AppCompatibilityRange`), and the descriptor-only install fast path (logo over HTTPS, failure
  tolerated). v1 custom feeds keep working end-to-end.**

### Consolidate local plugin state into a single registry document

- Severity: Medium (removes the folder/state/feed drift class; larger refactor of
  Catalog/Installer/Seeder seams)
- Evidence: ADR-0015 and the registry discussion (2026-11-14): three sources of truth exist today
  — the plugins folder, `plugin-state.json`, and the feed; content hashes exist only for
  built-ins (`PluginSeeder` three-hash logic), so installed plugins are never re-verified on scan.
- Proposed remedy: one registry document as the single local source of truth (version, origin,
  contentHash for every installed plugin, enabled, timestamps), tolerant read with adoption of
  existing `plugin-state.json` contents — no migration runner (ADR-0014); scan verifies hashes and
  surfaces user-modified descriptors uniformly.
- Trigger: after the client v2 slice; ideally before background sync, which wants the registry's
  sync metadata.
- **Slice complete (2026-11-14, `plugin-registry-consolidation`): `registry.json` is the single
  local source of truth — tolerant reads, atomic writes, one-time adoption of
  `plugin-state.json` (inverted `disabled` → `enabled`, deleted on success, retry on failure,
  corrupt-registry recovery), `contentHash` coverage for every managed plugin, scan-time
  verification exposing user-modified descriptors via `IPluginCatalog.GetUserModifiedPlugins()`,
  folder adoption with origin `unknown`, and gone-folder entries kept (see the prune-semantics
  entry below). `feedUrlAtInstall` is deferred to the background-sync slice.**

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

### Background feed sync and update badges

- Severity: Low (quality-of-life; update availability becomes visible without opening Browse)
- Evidence: the feed is fetched only when the Browse Plugins dialog opens
  (`BrowsePluginsDialogViewModel.LoadAsync`); no background refresh, no caching, no
  "update available" surface outside the dialog.
- Proposed remedy: a feed sync service (refresh on start plus an interval, ETag/If-None-Match,
  last-good index cache), a domain event on the existing bus, and an update badge on the
  affected agent card(s).
- Trigger: after the registry consolidation slice.
- **Slice complete (2026-11-14, `plugin-feed-background-sync`): `PluginFeedSyncService` refreshes
  on start plus a fixed hourly interval off the UI thread, conditional fetch (`If-None-Match`,
  `If-Modified-Since` fallback, 304 = unchanged), `cache/feed-index.json` + `cache/feed-sync.json`
  last-good cache with stale-fallback in Browse, tuple change detection, `PluginFeedUpdatedEvent`
  on the existing bus, and the shared update chip gaining plugin versions (agent update wins the
  slot). Transition-window refreshes persist no hash (verifier finding a).**

### Index signing for the v2 feed

- Severity: Low (trust hardening; the trust anchor is currently "whoever controls the repository")
- Evidence: ADR-0015 reserves `index.sig` and a null `signature` field; the signing slice was
  deliberately deferred.
- Proposed remedy: generate a signing key pair, sign `index.json` in the CI workflow (key in
  Actions secrets), and verify the signature on the client before parsing; treat a missing
  signature as valid only during a defined transition window. **Implementation uses ECDSA P-256,
  not Ed25519 — .NET 10 has no standalone Ed25519 signer (ADR-0015 amendment, 2026-11-14).**
- Trigger: after the default switch has settled; can proceed in parallel with later slices.
- **Slice complete (2026-11-14, `plugin-index-signing` + `embed-signing-key`): client verifies
  `index.sig` (ECDSA P-256/SHA-256, RFC 3279 DER, JSON envelope with the `algorithm` rotation
  field) before parsing, trust anchor embedded, custom feeds skip verification, transition
  window open until 2026-12-15 (`RequireFeedSignature` flips in a later slice). Repo side:
  `tools/sign-index.ps1` (sign + `-Verify`), LF-pinned index files, CI signing with the
  `INDEX_SIGNING_KEY` secret, `public.pem` published, first signature live and verified
  end-to-end. Release gate satisfied 2026-11-14.**

## Plugin lifecycle

Independent gaps surfaced by the 2026-11-14 audit of Proposal 5 (rejected as a load/validate facade;
see ADR-0016). Each is parked here rather than being absorbed into a new lifecycle abstraction.

### Built-in repair never runs after startup

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

### Stale prose comment quotes the retired "Detecting…" literal

**Status: Resolved by `quick-wins-sweep` (2026-11-12, commit `ad74bd9`, archived `openspec/changes/archive/2026-11-12-quick-wins-sweep`) — comment now reads "Detecting" (state name).**

- Severity: Low (comment-only; no assertion or behavior affected)
- Evidence: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs:347`
  still says `// leaving the cards it announced stuck on "Detecting…" forever.` — found during
  `busy-marker-control` verification; the change renamed the UI constant to `"Detecting"`
  (`AgentVersionText.Detecting`) but this comment documents the Detecting *state*, so it was left
  untouched to keep the change's file scope clean.
- Proposed remedy: update the quoted string to `"Detecting"` (or reword to name the state, not the
  literal) whenever the file is next touched for real work; no dedicated change is warranted.
- Trigger: any edit to `AgentManagementViewModelTests.Detection.cs`.

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

### Release-gate test can degrade on a starved CI runner (`GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId`)

**Status: Resolved by `release-gate-commandline-flake` (2026-11-12, commit `dc78f7f`, archived `openspec/changes/archive/2026-11-12-release-gate-commandline-flake`) — happy path now uses `Timeout.InfiniteTimeSpan` while dedicated 200 ms test keeps covering degradation.**

- Severity: Medium (blocks a release run when it fires; one occurrence in 6 release runs)
- Evidence: the v1.1.3 Release workflow (run `42979752090`, 2026-11-11) failed its test job with
  `WindowsAgentProcessInspectorTests.GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId`
  (`Assert.Equal` expected the injected `node.exe ...` command line, actual null). The test uses the
  default 2-second `commandLineTimeout` (`WindowsAgentProcessInspectorTests.cs:132`); the inspector's
  bulk command-line query runs through `Task.Run` + `Wait(budget)` and degrades to null command lines
  when the budget expires (`WindowsAgentProcessInspector.cs:26`, `:170-200`). On the loaded runner
  (three test assemblies in parallel right after the Release build) the stub query exceeded the
  budget, the degradation path fired, and the happy-path join assertion saw null. The same test is
  green locally in Debug and Release, green in the Debug CI run on the same commit (`c24fed5`), and
  green in all five previous release runs (v1.0.6–v1.1.2).
- Proposed remedy: make the happy-path test independent of the wall clock — inject an effectively
  unbounded budget (`Timeout.InfiniteTimeSpan`) for this test while the dedicated 200 ms timeout test
  (`WindowsAgentProcessInspectorTests.cs:211`) keeps covering the degradation path; optionally assert
  the source was called and did not time out before the join assertion so a timeout cannot masquerade
  as a join bug. Test-only change.
- Trigger: the next Release workflow test-job failure naming this test (re-run the failed job to
  unblock the release; then apply the remedy as a small test-only change).

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

### Plugin feed cancellation is reported as a download failure

**Status: Resolved by `feed-redirect-hop-limit` (2026-11-14)** — both sub-items addressed: cancellation was already resolved (`src/CLIHub.Core/Plugins/PluginFeedClient.cs:794-796` already rethrows `OperationCanceledException when (cancellationToken.IsCancellationRequested)` before the general catch, so caller-requested cancellation propagates correctly); the redirect off-by-one is fixed by the loop correction in `PluginFeedClient.SendFollowingRedirectsAsync` (`hop <= MaxRedirectHops`).

- Severity: Low (edge-case UX; no data loss, temp files are cleaned)
- Evidence (2026-11-04): `src/CLIHub.Core/Plugins/PluginFeedClient.cs` catches timeout cancellation
  with a `when (!cancellationToken.IsCancellationRequested)` filter, but the subsequent general
  `catch (Exception)` intercepts external cancellation and returns a generic "Could not load plugin
  feed" / "Package download failed" result instead of letting the caller observe cancellation.
  `FollowRedirectsAsync` also allows 5 HEAD requests (`hop < MaxRedirectHops`), so a chain of five
  redirects is rejected as "more than 5 hops" while the docs say "up to 5 hops".
- Proposed remedy: add
  `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }`
  before the general catch in both methods, and align the redirect loop/message with the documented
  5-redirect limit.
- Trigger: the next change touching `PluginFeedClient`, or a user report of a spurious error dialog
  when cancelling a feed request.

### plugin-seeding spec example still names a feed agent

**Status: Resolved by `spec-copy-accuracy-sweep` (2026-11-14)** — replaced `copilot --continue` with `omp --continue` in the "Commands match the agent CLI surface" scenario.

- Severity: Low (documentation accuracy only; the example is illustrative)
- Evidence (2026-11-05): `openspec/specs/plugin-seeding/spec.md:322` — the "Commands match the agent
  CLI surface" scenario uses `copilot --continue` as its example, but GitHub Copilot is no longer a
  built-in agent as of 1.1.1 (it is a feed plugin). The three chained changes carried the example
  over deliberately to keep their deltas minimal, but a reader can now mistake a feed agent for a
  built-in.
- Proposed remedy: in the next change that touches `plugin-seeding`, replace the example with a
  built-in agent (for example `omp --continue` or `devin --continue`) or phrase it without naming an
  agent.
- Trigger: the next spec change to `plugin-seeding`, or a documentation review.

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

### Plugin-management bindings bypass the agent-management interface

**Status: Resolved by `followups-quick-wins` (2026-11-14)** — added `InvalidCards`, `DisabledCards`, `HasDisabledPlugins`, `PluginCatalogSummary`, and `EnablePluginCommand` to `IAgentManagementSection` (all were already present in `AgentManagementViewModel`).

- Severity: Low (maintenance and type-safety; the UI works through WPF reflection)
- Evidence (2026-11-04): `src/CLIHub/Views/SettingsAgentsPage.xaml:329-399` binds
  `AgentsState.DisabledCards`, `HasDisabledPlugins`, and `EnablePluginCommand`;
  `src/CLIHub/Views/SettingsWindow.xaml:219` binds `PluginCatalogSummary`. Those members (and the
  older `InvalidCards`) are declared only on `AgentManagementViewModel`, while
  `SettingsViewModel.AgentsState` is typed as `IAgentManagementSection`
  (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:188`). WPF resolves them at runtime, so the
  page works, but interface-typed consumers cannot see them and the interface documentation is
  inaccurate. `AvailabilitySummary` (`IAgentManagementSection.cs:32`) is no longer bound anywhere
  after the AGENTS summary card switched to `PluginCatalogSummary`.
- Proposed remedy: add the plugin-management members (and `InvalidCards`) to
  `IAgentManagementSection`, or extract a narrower plugin surface; decide whether
  `AvailabilitySummary` is still needed or remove it with its tests.
- Trigger: the next change touching `IAgentManagementSection`, the CLI Agents page, or any
  non-XAML consumer of `AgentsState`.
- Re-checked (2026-11-05): `settings-agents-actions-menu` added `PluginActions` on
  `AgentManagementViewModel` only and binds it through the interface-typed `AgentsState`, so this
  pre-existing pattern gained one more member; the evidence and trigger stand.

### Release notes requirement says "status card" but the link is on the title row

**Status: Resolved by `spec-copy-accuracy-sweep` (2026-11-14)** — reworded requirement body from "from the status card" to "from the title row"; the link lives in `UpdatesHeaderTitleExtra` on the page title row.

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

**Status: Resolved by `a11y-sweep` (2026-11-14)** — `CLIHub.Controls.DecorativeGlyph` (a `TextBlock` subclass returning a null automation peer) is applied to the 20 decorative glyph sites across the Settings pages, sidebar, page header, summary/setting cards, status chip, input decorations and Actions-menu item glyphs; icon-only buttons keep their glyphs and their accessible names.

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

### Actions-menu entries lost the removed buttons' tooltips and accessible names

**Status: Resolved by `a11y-sweep` (2026-11-14)** — `MenuAction` gained optional `Tooltip` and `AccessibleName` (with an `EffectiveAccessibleName` label fallback); both windows' action templates bind them, and the Settings ACTIONS toggle is named "Plugin catalog actions".

- Severity: Low (accessibility and discoverability parity regression; no functional impact)
- Evidence (2026-11-05): the four plugin-management buttons removed from the CLI Agents header by
  `settings-agents-actions-menu` carried `ToolTip` and `AutomationProperties.Name` attributes
  (`src/CLIHub/Views/SettingsAgentsPage.xaml`, pre-change lines 144-203). Their Actions-menu
  replacements reuse `MenuAction` (`src/CLIHub/ViewModels/Shared/MenuAction.cs`), which has no tooltip
  or accessible-name fields, and the new `ACTIONS` toggle carries only its `Content` text. This
  matches the Launch window's existing Actions menus, which also expose only label text.
- Proposed remedy: add optional `ToolTip` and accessible-name fields to `MenuAction` and bind them in
  both views' action-entry templates, or at minimum give the toggle an
  `AutomationProperties.Name` such as "Plugin catalog actions".
- Trigger: an accessibility audit, or the next change touching `MenuAction` or the Actions-menu item
  templates.

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

### Two glyph constants went unused after the Agent Detection icon swap

**Status: Already resolved** — `src/CLIHub/Theming/IconGlyphs.cs` no longer contains the `Diagnostic` or `Process` constants; they were already removed.

- Severity: Low (dead constants only; no behavioral or build impact, and the compiler does not warn
  on an unused `const` member)
- Evidence (2026-11-08): the `agent-detection-page-icons` change replaced every reference to
  `IconGlyphs.Diagnostic` (E9D9, five positions) and `IconGlyphs.Process` (E9F3, the page entry) on
  the Settings -> Agent Detection page. Both declarations remain at
  `src/CLIHub/Theming/IconGlyphs.cs:24-25` with zero references anywhere in `src/` or `tests/`;
  `IconGlyphs` is the single glyph catalog, so an unreferenced member is dead weight rather than a
  bug.
- Proposed remedy: delete both constants, or keep them deliberately as catalog entries if another
  page is expected to reuse the glyph.
- Trigger: the next change that touches `IconGlyphs.cs`, or a dead-code sweep.

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

### Settings maximize button keeps a static "Maximize" tooltip when maximized

**Status: Already resolved** — `src/CLIHub/Views/WindowHeaderBar.xaml:67-74` already switches the tooltip between "Maximize" and "Restore" via a `WindowState` data trigger.

- Severity: Low (cosmetic tooltip text; the glyph already flips to Restore)
- Evidence (2026-11-10): the maximize/restore button in `SettingsWindow` sets `ToolTip="Maximize"`
  statically at `src/CLIHub/Views/SettingsWindow.xaml:299`. After maximizing, the tooltip still reads
  "Maximize" while the button restores. The Browse Plugins dialog no longer has a maximize button.
- Proposed remedy: switch the tooltip with the same `WindowState` trigger used for the glyph.
- Trigger: an accessibility/tooltip polish pass, or the next change that touches the Settings header.

### Pattern-chip remove button gives screen readers no hint before reveal

**Status: Resolved by `a11y-sweep` (2026-11-14)** — the focusable chip border carries a persistent `AutomationProperties.Name` ("Ignored pattern <name>, Tab reveals the remove button") bound with a `StringFormat`, so the affordance is announced before the remove button is revealed.

- Severity: Low (mouse and keyboard flows work; only the screen-reader discovery path is affected)
- Evidence (2026-11-11, `pattern-chip-hover-remove`): the ignored-pattern chip's remove button is
  `Visibility="Collapsed"` until the chip is hovered or keyboard-focused
  (`src/CLIHub/Views/SettingsProjectsPage.xaml` chip `ControlTemplate` triggers), so UIA/screen
  readers announce "Remove pattern" only while it is revealed. The chip itself carries no persistent
  automation name hinting that a remove action exists or that Tab reveals it — recorded as the
  resolved "screen reader announcement" open question in the change's design.md.
- Proposed remedy: set a persistent `AutomationProperties.Name` on the focusable chip border, e.g.
  "Ignored pattern &lt;name&gt;, Tab reveals the remove button", so screen-reader users can discover
  the affordance before revealing it.
- Trigger: an accessibility requirement or audit, or the next change touching chip automation names.

### Maximize work-area clamp writes a copy that is never marshalled back

**Status: Resolved by `followups-quick-wins-2` — dead `ptMax*` assignments removed; monitor lookup block removed; minimum tracking size behavior unchanged; XML doc updated to accurately describe actual behavior; smoke test evidence recorded.**

- Severity: Low (dead window-message writes; runtime behavior verified fine by smoke, code-hygiene
  issue only)
- Evidence (2026-11-13, `add-chrome-window-base` verification): the unified `WM_GETMINMAXINFO`
  handler in `ChromeWindow.cs:110-137` (the Settings shape, byte-identical to HEAD's
  `SettingsWindow`) assigns `ptMaxPosition`/`ptMaxSize` on the managed `MINMAXINFO` copy but never
  marshals it back to `lParam`; the subsequent `WindowChromeHelper.ApplyMinTrackSize`
  (`src/CLIHub/Views/WindowChromeHelper.cs:74-77`) re-reads the structure from `lParam` and writes
  back the **original** `ptMax` values with only `ptMinTrackSize` changed, so the clamp assignments
  are dead writes. Runtime smoke (2026-11-13, user-confirmed) showed a maximized Settings window
  keeps the taskbar visible, so the dead assignments currently cause no user-visible problem; the
  intended clamp simply never applies.
- Proposed remedy: either remove the dead `ptMax*` assignments (and the now-unneeded
  `MonitorFromWindow`/`GetMonitorInfo` calls) with the smoke evidence recorded, or make the handler
  marshal the clamped structure back once if the clamp is actually wanted for multi-monitor/DPI
  edge cases — decide which contract the base class documents.
- Trigger: the next change touching `ChromeWindow`'s window-message handling, or a report of a
  maximized window covering the taskbar on a multi-monitor or DPI-scaled setup.
