# Remove AvailabilitySummary and its backing counter

## Why

`IAgentManagementSection.AvailabilitySummary` (`IAgentManagementSection.cs:45-48`) builds a "N supported · M not installed" string that has **zero XAML bindings**. The AGENTS summary card (`SettingsWindow.xaml:201-204`) binds `InstalledAgentCount` + `PluginCatalogSummary`, and the General card (`:162-165`) binds `AgentsInstalledCount` + `AgentsSupportSummary`. Its doc comment ("shown under the installed count") is stale. The property and its backing `NotInstalledAgentCount` are dead surface.

The backlog entry at `docs/backlog.md:327-339` ("AvailabilitySummary is declared but unbound") explicitly calls for removal when the next change touches `IAgentManagementSection`.

## What Changes

1. **Remove `AvailabilitySummary`** from `IAgentManagementSection` and `AgentManagementViewModel` — the property, its doc comment, and its `OnPropertyChanged` raise in `RefreshSummarySurfaces()`.
2. **Remove `NotInstalledAgentCount`** — it exists only to feed `AvailabilitySummary` and is consumed nowhere else (grep: four matches, all in `AgentManagementViewModel.cs`).
3. **Rename `RefreshAvailabilitySummary()` → `RefreshSummarySurfaces()`** — the method raises 11 derived summary properties (counts, Update aggregation, default-agent detail, disabled-plugin gate, plugin catalog summary); the old name is now misleading. All 10 call sites are in the same file; the rename is mechanical.
4. **Migrate the eight test assertions** from `AvailabilitySummary` strings to `SupportedAgentCount`/`InstalledAgentCount` counts, preserving the load/detect coverage rather than deleting it. The PropertyChanged assertion at `Detection.cs:286` switches to `InstalledAgentCount`.
5. **Resolve the backlog entry** with a `**Status: Resolved by remove-availability-summary.**` line.

## Impact

**User-visible:** None. The removed property has no binding; the two Settings summary cards continue to display their existing sources unchanged.

**Code changes:**
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — remove `AvailabilitySummary` (lines 45-48)
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — remove `NotInstalledAgentCount` (lines 238-241) and `AvailabilitySummary` (lines 243-249); rename `RefreshAvailabilitySummary` (line 800) to `RefreshSummarySurfaces` and update 10 call sites; remove two `OnPropertyChanged` lines (804-805)
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Load.cs` — 3 string assertions replaced/removed (lines 37, 161, 174)
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs` — 5 assertions replaced/removed (lines 27, 40, 64, 71, 286)
- `docs/backlog.md` — resolve entry at lines 327-339

**Spec deltas:** None (`skip_specs: true`) — this is an internal-only removal of dead surface.

## Explicit Non-Goals

- Do not remove `SupportedAgentCount` or `InstalledAgentCount` — `SettingsViewModel` feeds both summary cards from these.
- Do not touch `PluginCatalogSummary` — it is bound on the AGENTS card and stays.
- Do not change the Agents page layout or bindings.
- Do not remove `DefaultAgentDetail`, `Updates*`, `HasDisabledPlugins` from the refresh method — they stay.

## Dependencies

None. Self-contained cleanup.