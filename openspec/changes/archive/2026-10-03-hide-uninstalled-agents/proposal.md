# Hide agents that are not installed on the host

## Why

The launch window lists every seeded plugin regardless of whether the agent's CLI is present on the machine. A user who does not have Qwen Code installed still sees it in the agent list, clicks it, and gets a launch failure. Detection already knows whether an agent is installed in the system (`IAgentDetectionService.IsInstalledInSystem`), but the launch window never consults it when building the list.

## What Changes

- The agent list in the launch window SHALL only include agents whose plugin declares at least one existing system path (installed on the host).
- The host-installed filter is unconditional: it applies whether or not the "show only project agents" preference is enabled, and whether or not a project is selected.
- The availability display (dimming for project-unavailable agents, optional project filter) keeps working on top of the installed filter: first uninstalled agents are removed, then project availability dimming/filtering applies to what remains.
- No changes to detection itself, plugin format, or the persisted preferences.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `agent-availability-display`: add a requirement that agents not installed on the host are excluded from the agent list, ahead of the existing dimming and optional project-filter requirements.

## Impact

- `src/CLIHub.Core/Services/AgentListComposer.cs` — new Core helper that composes the visible list (installation gate, then optional project filter); unit-tested in `tests/CLIHub.Tests`.
- `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` — `RefreshAgents` now delegates list composition to the helper instead of iterating plugins itself.
- `src/CLIHub/Windows/MainWindow.xaml.cs` — legacy, never instantiated; left as-is (out of scope).
