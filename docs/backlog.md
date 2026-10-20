# Backlog

Parked findings and deferred work. Each item records evidence, severity, a proposed remedy, and the
trigger that should bring it back. The pipeline protocol (`pipeline-protocol` skill) reviews this
file during `/pipe-resume`; do not re-derive parked work from conversation history.

## Agent and process domain

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
