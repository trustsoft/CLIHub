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
- Decision recorded in `openspec/changes/fix-settings-open-delay/design.md` (D2, rejected there).

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
  (`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:260-295`) and `LocatePowerShell7` (`:335-361`)
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
  (`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:86-91`) returns the first non-empty value across
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
  (found while refreshing `openspec/changes/git-bash-resolution`; explicitly out of its scope).

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
