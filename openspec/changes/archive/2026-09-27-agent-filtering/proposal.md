## Why

Dimming unavailable agents helps, but with many agents the list still contains rows that are irrelevant to the current project. A toggle to hide unavailable agents (keeping dim as the default) gives a focused list on demand. The choice should persist across sessions.

## What Changes

- Add an optional filter that hides agents not available in the current project
- Keep dimming as the default behavior when the filter is off
- Add the user preference `ShowOnlyProjectAgents` (default `false`) to `AppPreferences`, persisted in `config.json`
- Add a toggle in the agent panel to switch between dim and hide
- Filtering applies only when a project is selected; with no project, all agents are shown

## Capabilities

### New Capabilities

<!-- None: extends the existing availability display with an optional filter. -->

### Modified Capabilities

- `agent-availability-display`: adds an optional "hide unavailable" mode that takes precedence over dimming when enabled

## Impact

**Modified code:**
- `src/CLIHub.Core/Models/AppConfig.cs` — add `AppPreferences.ShowOnlyProjectAgents` (default false)
- `src/CLIHub/Windows/MainWindow.xaml` — add a filter toggle in the agents panel
- `src/CLIHub/Windows/MainWindow.xaml.cs` — read/persist the preference; apply filter in `RefreshAgents`

**Reuses:**
- `IAgentDetectionService.IsAvailableInProject` and the existing dimming logic

**No breaking changes** — the new config field is optional and defaults to the current behavior.
