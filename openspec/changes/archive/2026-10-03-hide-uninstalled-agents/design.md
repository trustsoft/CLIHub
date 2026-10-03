## Context

`LaunchWindowViewModel.RefreshAgents` (src/CLIHub/ViewModels/LaunchWindowViewModel.cs:401) builds `Agents` from `_pluginManager.GetAllPlugins()`, applying only the optional project-availability filter. It never calls `IAgentDetectionService.IsInstalledInSystem`, so uninstalled agents stay visible. `MainWindow` also builds an agent list, but it is never instantiated — dead code out of scope. Detection results are cached with the existing TTL, so an extra check per plugin per refresh is cheap.

## Goals / Non-Goals

**Goals:**
- Filter the launch-window agent list by host installation, ahead of the project filter and dimming.

**Non-Goals:**
- No changes to detection logic, plugin format, or preferences schema.
- No cleanup of the legacy `MainWindow` window.

## Decisions

- **Compose the list in a Core helper (`AgentListComposer`), called from `RefreshAgents`.** The static helper drops plugins that are not installed, then applies the optional project filter, returning `(Plugin, IsAvailable)` entries. Chosen during apply: the ViewModel lives in the WPF project that `CLIHub.Tests` cannot reference (tests reference Core only), so testing the filter inside the ViewModel was impossible without restructuring the test project. A Core helper keeps the "tests reference Core only" convention and lets the ordering rule (installation gate before project filter) be unit-tested with a mocked `IAgentDetectionService`.
  - Alternative: filter inline in `RefreshAgents` (original decision) — rejected because it left the behavior untestable in the existing test project.
- **`AgentItem` and the ViewModel shape stay untouched.** `RefreshAgents` maps composer entries to `AgentItem` exactly as before; dimming, selection restore, and version population are unchanged.
- **Rely on the existing detection cache and refresh path.** `Refresh` already calls `_agentDetectionService.Invalidate()` before `RefreshAgents()`, which satisfies the "refresh re-evaluates installation" scenario for free.

## Risks / Trade-offs

- [A plugin with missing or empty `detection.systemPaths` is treated as not installed] → This is the documented meaning of detection; seeded plugins declare system paths. If a real plugin ships without them it disappears from the list — acceptable, and consistent with the `agent-detection` spec ("No marker present → not installed").
- [Stale cache can briefly show/hide an agent after install/remove outside the app] → Mitigated by the existing TTL and manual Refresh; unchanged behavior from the availability checks.
