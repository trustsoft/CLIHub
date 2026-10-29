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
  services and domain events; Phase 2 (domain refinement) — service decomposition, **process domain [Phase 1+2 COMPLETE]**,
  plugin lifecycle; Phase 3 (configuration evolution) — internal decomposition. Immediate actions
  (next 30 days): document architecture principles, extract `ILoggingContextProvider`, implement
  first domain event POC (`ProjectSelected`). See `docs/architecture-analysis-index.md` for full
  navigation. **Proposal 4 Phase 1+2 complete 2026-10-29 (commit a64422c); Phase 3 (consumer migration) deferred;
  see ADR-0012, implementation report, and archived change.**
- Trigger: Q1 2027 planning cycle, onboarding new contributors, or next major refactoring that would
  benefit from architectural clarity.

## Agent and process domain

### Process domain Phase 3: consumer migration (deferred)

- Severity: Low (architecture improvement, not blocking; Phase 1+2 complete and working)
- Evidence: Process domain model unified via `IProcessOrchestrator` (commit `a64422c`, 2026-10-29).
  Domain models (`RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`) and orchestrator
  implementation complete with adapters wrapping existing infrastructure. All 1,221 tests passing.
  Change archived as `openspec/changes/archive/2026-10-29-process-domain-model/`. Four consumers still
  use fragmented Infrastructure.Processes services directly:
  - `src/CLIHub.Core/Workflows/AgentCommandService.cs` — injects 5 process services
  - `src/CLIHub.Core/Workflows/LaunchCommandCoordinator.cs` — uses IInteractiveProcessRunner
  - `src/CLIHub/Coordinators/WindowActionCoordinator.cs` — uses IInteractiveProcessRunner
  - `src/CLIHub.Core/Infrastructure/Monitoring/AgentProcessMonitor.cs` — uses IAgentProcessInspector
- Proposed remedy: Migrate consumers to `IProcessOrchestrator` (Phase 3 of Proposal 4). For each:
  replace multiple service dependencies with single orchestrator dependency, adapt to domain types
  (`LaunchRequest`, `ProcessResult`), verify behavior unchanged, update tests. Estimated 2-3 days.
  See `docs/proposals/proposal-004-process-domain-model.md` (section "Phase 3: Consumer Migration").
- Trigger: Next refactoring touching any of the four consumers, reducing test mock complexity, or
  completing Phase 1 prerequisites (Proposal 1: cross-cutting services).

### Process orchestrator unit tests (deferred)

- Severity: Low (quality improvement; functionality covered by 1,221 existing integration tests)
- Evidence: Process domain implementation (commit `a64422c`, 2026-10-29) includes orchestrator and
  adapters but defers comprehensive unit test coverage. Test directory structure created at
  `tests/CLIHub.Tests/Core/Processes/`, but planned test classes for `ProcessOrchestrator`,
  `RuntimeResolver`, `CommandLineBuilderAdapter`, and `AgentProcessFinder` were not written due to
  time/token budget constraints. All functionality verified through existing tests (build + 1,221
  tests passing). See Stage 7 in `openspec/changes/archive/2026-10-29-process-domain-model/tasks.md`.
- Proposed remedy: Add unit tests for:
  - `ProcessOrchestratorTests.cs` — LaunchInteractive/CaptureOutput/FindRunningAgents/ResolveRuntime
  - `RuntimeResolverTests.cs` — runtime resolution for all RuntimeKind values
  - `CommandLineBuilderAdapterTests.cs` — command line building for all runtimes
  - `AgentProcessFinderTests.cs` — process finding and Runtime mapping
  Each test class should cover happy path, error cases, cancellation, and edge cases. Estimated 2-3 hours.
- Trigger: Test coverage review, adding more orchestrator features, or during Phase 3 consumer migration
  when orchestrator behavior changes become complex.

### Flaky ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus (resolved)

- Status: Resolved by the `scanning-flake-settle` change (2026-10-24).
- Resolved model: the test's pre-`Stop()` wait now settles on the service's real observables —
  `Watchers.Count == 1 && Status.State == Idle && Status.ScannedDirectoryCount == 2` — which only
  the scan-final status post can publish in this scenario, so `Stop()`'s `NotConfigured` write is
  deterministically the final one (the race is removed structurally, not narrowed). Post-`Stop`
  asserts are byte-identical; no production change — `Stop()` has zero production callers (only
  the two sibling tests).
- Guarded by: the rewritten test itself, ten consecutive green full-solution trx-logged runs
  (2026-10-24, ~15 s each, 1140/0 each; corroborated by 30 on-disk trx artifacts with zero failed
  counters), and the structural publisher-enumeration argument in the change's design (D1) — the
  actual fix proof.
- Trigger: a recurrence report of this test failing under parallel load.

### Async blocked-state population on the agents page (resolved)

- Status: Resolved by the `async-blocked-state` change (2026-10-24). Formerly parked as design D2
  of fix-settings-open-delay.
- Resolved model: management cards build with `CanUpdate = declaresUpdate` (the declarative
  interim); a sibling blocked-state pass in the canonical population shape (own
  `blockedStateCts`/`blockedStateGeneration`, operation name "Agent blocked-state population")
  starts at `Load` only when a plugin declares an `update` command, enumerates via `Task.Run` off
  the UI thread, and — under the token + generation guard — writes `CanUpdate` per card plus the
  blocked `Message` only when blocked (never clearing). A re-`Load` or page exit cancels the pass;
  an in-flight enumeration may complete but its result is not applied. Enumeration failures log
  through the monitor and the cards settle fail-open at declarative availability. The
  `SettingsWindow.ShowSettings` restructure listed in the parked remedy was evaluated and dropped
  as unnecessary: the pass removes the load-blocking cost, and load-before-show serves every page.
- Guarded by: `Load_ShowsDeclarativeAvailability_BeforeBlockedStateLands`,
  `Dispose_CancelsBlockedStatePass`, `Load_SecondLoadCancelsBlockedStatePass`, and
  `Load_WhenEnumerationFails_KeepsDeclaredAvailability`, plus the unchanged
  `Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock`,
  `Load_OtherAgentsProcessRunning_KeepsUpdateAvailableWithoutMessage`,
  `Load_WithNoRunningProcesses_KeepsDeclaredUpdateAvailableWithoutMessage`,
  `Load_MultipleUpdateDeclaringPlugins_EnumeratesProcessesOnce`,
  `Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses`,
  `Load_BuildsCardsCountsAndDefaultOptions`, and
  `Load_UnknownAvailability_LeavesCanUpdateUnchanged`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`).
- Trigger: a regression report that the blocked state never lands or lands on a closed surface, or
  any future edit that adds cost to the running-agent enumeration path.

### Monitor-level running-agent cache (resolved)

- Status: Resolved by design, no code — the `commandline-agent-blocking` tier restructure
  (2026-10-24) is the resolution.
- Resolved model: the requery availability check runs the cheap executable-name tier (no WMI,
  spec-pinned advisory), both execution-time gates run the full command-line tier off the UI
  thread under the 2-second WMI budget, and the Settings blocked-state pass is async and
  cancellable; the residual per-requery cost is one name-filtered `Process.GetProcesses`
  snapshot. A cache was evaluated and rejected: gates must never be stale
  (`agent-process-matching` "Execution-time checks use the command-line tier"), and an advisory
  TTL cache has no provably fresh key and no profiling evidence.
- Guarded by: the existing `WindowActionCoordinatorTests`/`AgentProcessMonitorTests` tier tests.
- Trigger (reopen): profiling shows the filtered enumeration as a hot spot on a requery path.

### Runtime-hosted agent blocking (Option B) (resolved)

- Status: Resolved by the `commandline-agent-blocking` change (2026-10-24). Provenance note: the user
  pulled this item out of the backlog on 2026-10-24 as a proactive product-gap decision, before its
  trigger fired — no user report of an unblocked update existed.
- Resolved model: `WindowsAgentProcessInspector` joins command lines onto the enumerated candidate
  processes with one bulk WMI query (`SELECT ProcessId, CommandLine FROM Win32_Process`, name-filtered
  to the candidates) behind an internal `IProcessCommandLineSource` seam, bounded by a hard 2-second
  wall-clock budget; on failure or timeout it logs one warning and the pass degrades to
  executable-name matching (fail-open, matching the enumeration-failure contract). Matching runs in
  two detail tiers (`AgentProcessMatchDetail`): the cheap executable-name tier never retrieves command
  lines, and the command-line tier — the default — additionally matches a process running inside a
  shared runtime host (`node`, `python`, `pythonw`) whose command line contains
  `node_modules\` + the plugin's declared npm `updateCheck` package + a trailing separator
  (OrdinalIgnoreCase, forward slashes normalized). The requery availability check
  (`WindowActionCoordinator.CanExecuteUpdate`) stays on the cheap tier as an advisory early-out, and
  both execution-time gates (`AgentManagementService.UpdateAsync`, moved into `Task.Run`, and the
  launch window's `ExecuteUpdate`, restructured as a fire-and-forget async flow) use the full tier, so
  a runtime-hosted agent blocks its update authoritatively even when the button was enabled.
- Guarded by: the Stage 1–4 tests — `AgentProcessMatcherTests` (command-line semantics and candidate
  derivation), `AgentProcessMonitorTests` (tier flow), `AgentManagementServiceTests`
  (`UpdateAsync_RuntimeHostedAgent_IsBlocked`, `UpdateAsync_UnrelatedNodeScript_StartsUpdate`),
  `WindowsAgentProcessInspectorTests` (join, timeout, degradation), `WindowActionCoordinatorTests`
  (advisory/gate split), and `Load_NodeShimAgentRunning_DisablesUpdateAndExplainsBlock`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`); real-WMI behavior and
  the live UI remain manual smoke.
- Trigger: a report of a runtime-hosted agent not blocking an update, or observed WMI-timeout
  pile-up (abandoned queries) on a machine with a hung Winmgmt.

### Version population loop consolidation (resolved)

- Status: Resolved by the `consolidate-version-population` change (2026-10-24).
- Resolved model: both live loops use the pane's shape — a generation counter plus a
  surface-owned `CancellationTokenSource` that is cancelled and replaced when a newer pass starts
  or the surface is disposed (`AgentPaneController`, `AgentManagementViewModel`), with parallel
  per-card probes under a linked refresh+application token and writes guarded by
  not-cancelled + current-generation. Closing the Settings window or a re-`Load` therefore
  cancels the management pass; an in-flight version command still completes and caches by design
  (`AgentVersionService` runs probes on `CancellationToken.None`), but its result is not applied.
  Unexpected population failures log through each surface's injected `ILogger<T>` (`LogError`);
  surface-local cancellation stays silent. The legacy `Views/MainWindow.xaml.cs` copy remains the
  documented untouched exception (the window is never constructed).
- Guarded by: `PopulateVersionsAsync_WhenProbeThrows_LogsError` and
  `Dispose_UnsubscribesAndCancelsVersionPopulation`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs`), plus
  `Dispose_CancelsInFlightPopulation`, `Load_SecondLoadCancelsFirstPass`,
  `PopulateVersions_WhenProbeThrows_LogsErrorAndKeepsOtherCards`, and the unchanged
  `Load_PopulatesVersions` / `Load_PopulatesUpdateAvailabilityPerCard`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`); the strengthened
  contract is recorded in the `agent-version` spec's "Stale version population does not update
  the current list" requirement.
- Trigger: a regression report that versions stay `…` after closing and re-opening Settings, or
  any future edit to a version-population loop (a third live surface should lift the shared shape).

### Magic version sentinels (resolved)

- Status: Resolved by the `agent-install-state` change (2026-10-25), superseding the `low-sweep`
  sentinel model.
- Resolved model: the string sentinels are gone. `Pending` (`"…"`), `Unknown` (`"unknown"`), and
  the `IsPending`/`IsKnown` checks were deleted from `AgentVersionText`
  (`src/CLIHub/Helpers/AgentVersionText.cs`); an unresolved version is now a null value, never
  placeholder text, and `AgentVersionText` survives only as the owner of the chip display texts
  ("Detecting…", "Not installed", "Installed"). Installation state is explicit:
  `AgentInstallState { Detecting, Installed, NotInstalled }` lives on `AgentCardItem` and
  `AgentOption` (with a derived `IsInstalled` convenience), card chips and the default-agent
  selector derive their text and styling from it, and version/availability work is gated on it
  (`AgentManagementService.ProbeAsync` runs no version command for a not-installed agent).
  `UpdateService`'s own "unknown" fallback for the application version stays out of domain.
- Guarded by: the state-driven tests in `AgentCardItemTests` (chip and selector `DisplayName`
  matrices), `AgentManagementViewModelTests` (gating, Detecting transitions and restore-on-throw,
  null-version assertions), and `AgentManagementServiceTests` (`ProbeAsync_UnknownVersion_ReturnsNullVersion`
  verifying the version service is never called for a not-installed agent).
- Trigger: a regression report of placeholder version text (`unknown` or `…`) anywhere in an agent
  list surface, a card stuck on "Detecting…", or any future edit that re-derives display state
  from a version string's shape.

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

### Per-card Detect interleaving with a header action drops that card's update indicator (resolved)

- Status: Resolved by the `independent-card-actions` change (2026-10-27) via the shared-busy-counter
  remedy: a per-card Detect (or Check updates) now blocks the header actions while it runs, so a
  header check can no longer snapshot a concurrently detecting card as `Detecting` and drop its
  update indicator.
- Guarded by: the per-card Detect/header gating facts `DetectCardCommand_WhilePerCardDetectRuns_DisablesOnlyTheActingCardAndGatesHeader`,
  `DetectCardCommand_RaisesCanExecuteChangedAtDetectStartAndEnd`, and the header pass tests in
  `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`.
- Trigger (reopen): a report of an update chip that disappears after pressing Detect on a card.

### Runtime executable PATH-scan identity (Git Bash and PowerShell 7) (resolved)

- Status: Resolved by the low-sweep-2 change (2026-10-24) via the spec-wording remedy; no
  production change.
- Resolved model: the `agent-commands` "Terminal vs captured execution" requirement now states
  the actual guarantee (fixed preference order, absolute path, WSL-launcher exclusion, PATH hits
  accepted without distribution verification for bash; pwsh PATH acceptance including the Store
  execution alias pinned by an ADDED scenario). Distribution validation of bash PATH hits was
  evaluated and rejected (heuristic false positives on Cygwin/MSYS2-with-git, false negatives on
  portable layouts, safe-fail regression risk).
- Guarded by: the unchanged locator facts
  `Locate_GitBash_PathEntryOutsideWindowsDirectoryIsUsed`,
  `Locate_GitBash_WslLauncherOnPath_ReturnsNull`,
  `Locate_PowerShell7_PathEntryIsUsed`,
  `Locate_PowerShell7_PathEntryInsideWindowsDirectoryIsUsed`.
- Trigger (reopen): a user report of a wrong bash/pwsh flavor being launched.

### Registry InstallPath precedence can shadow a valid install (resolved)

- Status: Resolved by the `low-sweep` change (2026-10-24).
- Resolved model: `WindowsRuntimeEnvironment.ReadGitForWindowsInstallPaths()` (plural) reads all
  four views (HKLM64 → HKLM32 → HKCU Default → HKCU Registry32) through the internal
  `CollectInstallPaths(views, reader)` seam, and the locator's registry step verifies each
  candidate (`bin\bash.exe` exists) and prefers the first valid one — preserving the
  registry → well-known → PATH order and the fall-through when no candidate verifies.
- Guarded by: `WindowsRuntimeEnvironmentTests` (three hermetic collector facts plus a guarded
  HKCU-only real-registry fact) and the locator facts
  `Locate_GitBash_FirstRegistryCandidateStale_SecondCandidateWins` /
  `Locate_GitBash_AllRegistryCandidatesStale_FallsThroughToWellKnownRoot`.
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

### Update-detection test gaps: timeout path and missing-installation indicator (resolved)

- Status: Resolved by the `low-sweep` change (2026-10-24).
- Resolved model: the `StubHandler` gained a token-respecting `Delay`, and
  `GetLatestVersion_RequestTimesOut_ReturnsNull` sets the 1-second `AgentProbeTimeoutSeconds`
  floor and pins the release-request timeout → null (`Unknown`) degradation;
  `UpdateAvailability_WhenNotInstalledWithDeclaredUpdateCheck_ShowsNoIndicator` pins the
  no-chip case (uninstalled agent + declared `updateCheck` → `Unknown` → null indicator text).
  No production change.
- Guarded by: those two tests (`tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs`).
- Trigger: a regression report of a stuck availability chip or a hanging update check.

## Project domain

### AddProject returns the live instance (resolved)

- Status: Resolved by the hygiene-sweep change (2026-10-24).
- Resolved model: `ProjectService.AddProject`/`AddDiscoveredProject` return `Copy()` at all four
  return points (each method's existing-project early return and fresh-add return), aligned with
  the read methods and the detached-copies invariant; the interface docs state it.
- Guarded by: the new `AddProject_MutatingReturnedProject_DoesNotChangeRegisteredState` plus the
  unchanged `AddProject_DuplicatePath_ReturnsExisting` /
  `AddDiscoveredProject_ExistingManual_DoesNotReclassify`
  (`tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`).
- Trigger: any project-domain change.

### Hand-rolled clone siblings (resolved)

- Status: Resolved by the `consolidate-clone-siblings` change (2026-10-24).
- Resolved model: `ConfigurationSnapshot.CloneProject` is deleted; `From`/`ToAppConfig` call
  `project.Copy()` directly, leaving `Project.Copy()` the one canonical project copy. The three
  remaining manual copies (`ClonePreferences`, `ProjectDocument.From`/`ToProject`, and
  `PreferencesDocument.From`/`ToPreferences`) cannot delegate — the document mappings are
  transformers and `ClonePreferences` is the only `AppPreferences` clone site — so each carries a
  guard comment requiring it to map every public property of its source model.
- Guarded by: the round-trip and census tests in
  `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs` (every public property
  of `Project` and `AppPreferences` round-trips through the document boundary, `From`/`ToAppConfig`,
  and `Clone`; mutating a clone's nested `WatchedDirectories`/`IgnoredPatterns`/
  `WatchedDirectorySettings` does not change the source; the reflection census fails when a property
  is missing from the covered lists) plus the repository-level
  `Read_MutatingReturnedSnapshotCollections_DoesNotChangeStoredState`
  (`tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs`).
- Trigger: a red round-trip or census test after adding a `Project`/`AppPreferences` property, or
  any future persistence-boundary change.

### ProjectState shares live items at the persistence boundary (resolved)

- Status: Resolved by the low-sweep-2 change (2026-10-24).
- Resolved model: all five `ProjectState` crossing points (`From`/`ApplyTo` ×2, `CopyTo`) copy
  projects with the canonical `Project.Copy()`, and `ApplyTo` stores a fresh list of copies, so
  the repository's cached snapshot never aliases `ProjectService`'s live `Project` instances and
  in-place service mutations (`SetCurrentProject`, `ToggleFavorite`) can no longer leak into
  stored state outside an update transaction.
- Guarded by: the five `ProjectStateTests` isolation facts and the two `ProjectStateStoreTests`
  facts (including the real-repository post-transaction mutation fact).
- Trigger: any future persistence-boundary change or a new `ProjectState` consumer.

### ProjectPathPolicy duplication (resolved)

- Status: Resolved by the hygiene-sweep change (2026-10-24).
- Resolved model: `ProjectPathPolicy` is public and `ProjectRegistryViewModel.TryRegisterProject`
  reuses `Normalize`/`AreEqual` instead of its hand-rolled normalize/compare. Accepted alignment:
  re-registering an already-registered drive/UNC root from the Settings Projects page now shows
  the duplicate error instead of silently no-opping through `AddProject`'s existing-project
  branch (both outcomes satisfy the spec's duplicate-folder scenarios).
- Guarded by: `ProjectPathPolicyTests`, the unchanged
  `RegisterCommand_WithAlreadyRegisteredPath_KeepsSingleRowAndReportsError`, and the new
  `RegisterCommand_WithAlreadyRegisteredRootPath_ShowsDuplicateErrorWithoutAdding` pin.
- Trigger: path handling changes.

## Specifications and docs

### `main-window-layout` names the wrong window (resolved)

- Status: Resolved by the `launch-window-layout-rename` change (2026-10-24).
- Resolved model: the capability was renamed to `launch-window-layout` (create + retire deltas in
  the change; `openspec archive` applies the rename, authorized by `retire_capabilities: true`)
  and its "the main window" wording became "the launch window" in the Purpose and the eight
  scenario WHEN lines; requirement and scenario names are byte-identical.
- Evidence: the capability describes the launch window (hairline divider, action popovers, DWM
  rounding, Escape behavior) while `MainWindow` is an unregistered legacy reference.
- Trigger: any `launch-window-layout` edit.

### `app-lifecycle`/`preferences-ui` say "main window" for the launch window (resolved)

- Status: Resolved by the `spec-wording-cosmetics` change (2026-10-25).
- Resolved model: all five occurrences normalized to "the launch window" via MODIFIED requirement
  deltas ("Single instance enforcement" and "Startup window visibility" in `app-lifecycle`;
  "Save applies preferences" in `preferences-ui`), with every scenario name and all other
  requirement content byte-identical.
- Guarded by: the live-spec census — a case-insensitive "main window" grep over `openspec/specs/`
  returns zero hits; the wording matches the sibling capabilities `launch-window-layout` and
  `launch-window-theme`.
- Trigger: a reintroduced "main window" occurrence in a live spec.

### `agent-ui` requirement naming style drift (resolved)

- Status: Resolved by the `spec-wording-cosmetics` change (2026-10-25).
- Resolved model: the ten existing ID-style headers (`REQ-AGENT-CAP-002…012` — the set is
  non-contiguous; 001 and 004 were removed historically) renamed via `## RENAMED Requirements`
  pairs to sentence-style names derived from each requirement's body (e.g. `Update command support
  detection`, `Resume command enablement for session-capable agents`,
  `Unconditional launch command availability`, `Plugin schema compatibility`); requirement bodies
  and all scenario names untouched.
- Guarded by: the live-spec census — no `REQ-AGENT-CAP-` header remains in
  `openspec/specs/agent-ui/spec.md`; the only live references are historical (archive, CHANGELOG
  history, the resolved backlog entries).
- Trigger: a new ID-style requirement header in a live spec.

## DI and lifetime

### Transient IDisposable chain retained by the root provider

- Severity: Low
- Evidence: each Settings open resolves a transient IDisposable chain from the root provider, and
  MS.DI retains transients until application exit.
- Proposed remedy: resolve through a factory that disposes explicitly, if memory profiles ever show
  growth.
- Trigger: a memory-growth report.

### Matcher's unused logger (resolved)

- Status: Resolved by the hygiene-sweep change (2026-10-24).
- Resolved model: the field, constructor, and `Microsoft.Extensions.Logging` using are removed;
  the matcher uses its implicit parameterless constructor and the DI registration is unchanged.
- Guarded by: the unchanged `AgentProcessMatcherTests`, `AgentProcessMonitorTests`,
  `AgentManagementServiceTests`, and `AgentManagementViewModelTests`.
- Trigger: any edit to `AgentProcessMatcher`'s construction shape or its DI registration.

### Registered-but-unused `RuntimeSelector` (resolved)

- Status: Resolved by the hygiene-sweep change (2026-10-24).
- Resolved model: the selector, its registration
  (`WindowsServiceCollectionExtensions.cs:38`), and `RuntimeSelectorTests` are deleted;
  `RuntimeType.WindowsTerminal` is dropped with them. Settings parsing is unaffected — it goes
  through `RuntimeKinds`, whose fallback resolves stale `defaultRuntime` tokens to PowerShell;
  `RuntimeType` never round-trips config.json.
- Guarded by: the full-solution build and test suite (the build is the proof that no residual
  `RuntimeSelector`/`RuntimeType.WindowsTerminal` reference exists).
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
  (stopwatch-bounded wait loop). During the `scanning-flake-settle` fix (2026-10-24), the captured
  recurring flake was exactly `ProjectScanningServiceTests.Stop_DisposesWatcherAndClearsStatus`
  (see its resolved entry), making it the probable identity of this incident too; the
  `UpdateCheckerTests`/`UpdateDownloaderTests` candidates stay unexcluded until a captured failure
  names one of them.
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

### `.gitnexus/` is excluded only by the local git exclude file (resolved)

- Status: Resolved by the hygiene-sweep change (2026-10-24).
- Resolved model: `.gitignore` ignores the whole `.gitnexus/` directory beside `.codegraph/` and
  `graphify-out/`; nothing under it was tracked and `run.cjs` stays untracked — it is bootstrapped
  per machine (`npx`/`bunx`/`pnpm dlx`). The local `.git/info/exclude` line stays as harmless
  duplication.
- Guarded by: `git check-ignore -v .gitnexus/run.cjs` resolving through `.gitignore` (not only the
  local exclude file).
- Trigger: the next GitNexus run on a fresh clone, or any `.gitignore` edit.

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
  and 0 px radii remain literals because each has one consumer and encodes no shared role; the
  legacy `Views/MainWindow.xaml` also keeps its pre-palette colours (allowlisted by
  `ThemeColorLiteralTests`).
- Proposed remedy: promote a value to a `Sizing.*` token when a second consumer appears; leave a
  single-use literal alone until then.
- Trigger: copy-pasting a view and reaching for the same literal.

### Windows personalization changes are not followed while the application runs (resolved)

- Status: Resolved by the `follow-windows-theme-changes` change (2026-10-24).
- Resolved model: `SystemThemeFollower` (`src/CLIHub/Themes/SystemThemeFollower.cs`) subscribes to
  `SystemEvents.UserPreferenceChanged` for the first-instance session — `ShellCoordinator` starts it
  with the startup dispatch during shell wiring and disposes it with the session, so a second
  instance never subscribes. The handler ignores the event category and filters by value: it re-reads
  the stored theme from config.json, and only when it parses to System and the freshly read
  `SystemTheme.IsLight()` differs from the last observed mode does it dispatch one palette re-apply
  through the shared `PaletteSwitcher` — the same path startup and a Settings save use, so System
  still never resolves to blue. Fixed Dark, Light, and Blue themes (and unknown values, which fall
  back to Dark) ignore personalization changes, and a successful follow logs one informational line
  (added by user decision on 2026-10-24, overriding the original silent-on-success design D7).
- Guarded by: `SystemThemeFollowerTests` (`tests/CLIHub.Tests/Themes/SystemThemeFollowerTests.cs`)
  plus the follower start/dispose assertions in `ShellCoordinatorTests` and
  `ApplicationSessionTests`; the live repaint under a real `Application` stays manual smoke per the
  documented test-host precedent.
- Trigger: a regression report that switching Windows light/dark leaves the System palette stale
  until restart or the next Settings save.

### Unversioned README screenshot capture harness (resolved)

- Status: Resolved 2026-10-26 by `docs/images/tools/Capture-ReadmeImages.ps1` and the debug-only
  `src/CLIHub/Views/ReadmeImageCapture.cs` path. The script backs up and restores
  `%APPDATA%\CLIHub\config.json` and `cache\logos.json`, seeds a throwaway mock workspace, renders
  the app windows in-process at 1.5× (so a locked or headless desktop is enough), applies the rounded
  mask, and regenerates the HTML images with headless Chrome.
- Former evidence: the 0.9.9 README refresh used an ad-hoc PowerShell harness kept only in `%TEMP%`
  (`capture-final.ps1`): it seeds `%APPDATA%\CLIHub\config.json` with a throwaway workspace, opens
  Settings through UI Automation, captures with `CopyFromScreen`, crops the 2 px ring, applies the
  12 px mask, and restores the config. `PrintWindow` intermittently returned black frames on
  2026-10-23, which is why the screen-copy path was added. `docs/images/README.md` records only the
  manual attributes (150% scaling, crop, mask), not the harness.

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
