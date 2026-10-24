# Consolidate the agent version-population loops

## Why

Agent version population exists in three hand-rolled copies. Two are live and disagree:

- `AgentPaneController` (launch-window agent pane) uses a generation counter plus a
  per-refresh `CancellationTokenSource`, runs probes in parallel, and cancels on supersession
  and dispose — but swallows unexpected failures into `Debug.WriteLine`
  (`src/CLIHub/ViewModels/Agents/AgentPaneController.cs:184-187`), so they never reach the
  Serilog file even though the loop already runs inside `IApplicationOperationLifetime.RunAsync`.
- `AgentManagementViewModel` (Settings → CLI Agents page) populates serially with no
  cancellation and no generation (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:259-283`):
  its `Dispose` only sets a flag (`:228-231`), so after the Settings window closes the pass keeps
  issuing per-card availability (release-request) checks until it drains, and its stale-result
  protection holds only by snapshot isolation rather than by an explicit guard.
- The third copy is the legacy `MainWindow` (`src/CLIHub/Views/MainWindow.xaml.cs:194-217`), a
  documented dead window that is never constructed (`:22-24`).

This is parked as "Version population loop consolidation" (`docs/backlog.md:62-70`, severity
Medium). The `agent-version` capability already promises that an obsolete population pass never
updates the current list (`openspec/specs/agent-version/spec.md:97-107`); today the management
page satisfies that by accident, and nothing cancels its pass when the surface goes away.

## What Changes

**Behavior:**

- The management page's version population adopts the pane's shape: a generation counter, a
  surface-owned `CancellationTokenSource` cancelled when a newer pass starts or the page is
  disposed, linked with the application token, and parallel per-card probes with
  generation-guarded writes.
- Closing the Settings window (or superseding the pass with a re-`Load`) cancels the outstanding
  pass: it starts no further version lookups or availability checks. A probe already in flight
  may complete and cache its result (the version service's probes are deliberately not
  abortable), but its result is not applied.
- Unexpected population failures on either live surface are logged through `ILogger<T>` instead
  of `Debug.WriteLine` (pane) and reach a surface-specific logger category instead of only the
  generic operation-lifetime catch (management). Expected cancellation stays silent.
- User-visible version display is unchanged: cards still render immediately with the `…`
  placeholder, fill in per agent as probes resolve, and show `unknown` when a version is not
  available.

**Code:**

- `AgentPaneController` gains an injected `ILogger<AgentPaneController>`; its `Debug.WriteLine`
  catch becomes `LogError`, and the now-unused `using System.Diagnostics` goes away.
- `AgentManagementViewModel` gains `versionPopulationCts`/`versionPopulationGeneration` fields, a
  rewritten `PopulateVersions`/`PopulateVersionsAsync` mirroring the pane's shape (keeping its
  surface-specific per-card availability check and final `RebuildDefaultAgentOptions`), a
  local expected-cancellation filter, and local failure logging through its existing injected
  logger; `Dispose` bumps the generation and cancels/disposes the CTS.
- No new shared abstraction: the two loops keep the same *shape*, not one shared method (the
  item types and per-card steps differ; both files live in the WPF project).
- The legacy `MainWindow` copy is untouched — documented exception, dead code.
- Spec delta: `agent-version`'s "Stale version population does not update the current list"
  requirement is modified (original scenario names preserved) to also require cancellation of an
  obsolete pass and logging of an unexpected pass failure.

## Impact

**User-visible:**

- Versions fill in faster on the management page (parallel probes instead of strictly serial)
  and closing Settings stops the page's outstanding background work. No text, layout, or
  placeholder changes on either surface.

**Internal:**

- `AgentPaneController`: one constructor parameter (`ILogger<AgentPaneController>`); no DI
  registration change (`AddLogging`/Serilog is already registered,
  `src/CLIHub/Composition/ServiceRegistration.cs:55`, and the controller is DI-created at
  `:108`).
- `AgentManagementViewModel`: two fields, rewritten private methods, extended `Dispose`; public
  surface unchanged.
- Test construction sites for `AgentPaneController` gain the logger argument
  (`AgentPaneControllerTests`, `StatusMessageCoordinatorTests`, `LaunchWindowViewModelBuilder`,
  and the examples in `tests/CLIHub.Tests/README.md`).
- `AgentVersionService`, `AgentUpdateAvailabilityService`, `ApplicationOperationLifetime`, and
  the legacy window are untouched.

**Tests:**

- New management-loop tests for dispose-cancellation, supersession-cancellation, and
  failure-logging; a pane test for failure logging; existing population tests stay green.

## Non-goals

- Touching the legacy `MainWindow.xaml.cs` in any way (dead window, documented exception).
- The "Magic version sentinels" backlog item (`docs/backlog.md:72-78`) — no `AgentVersionText`
  helper; the `…`/`unknown` literals stay.
- The "Async blocked-state population on the agents page" backlog item
  (`docs/backlog.md:20-35`) — `Load`'s synchronous running-agent enumeration is unchanged.
- Any change to probe semantics: a started version command still runs to its timeout and caches
  its result (`src/CLIHub.Core/Agents/AgentVersionService.cs:89-90`); only the awaiting pass is
  cancellable.
- Any UI/text/layout change, new setting, or new abstraction shared between the two loops.
- Changes to `Detect`, `Test`, `Update`, or `EditConfig` command paths beyond coexistence with
  the new guards.

## Dependencies

None — self-contained within the two live version-population loops. The most recent related
archived changes (`2026-10-20-fix-settings-open-delay`, `2026-10-20-agent-update-detection`)
touched adjacent code but left both loops in their current shape; no spec from another active
change is affected (there are no other active changes).
