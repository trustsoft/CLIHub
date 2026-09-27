## Why

The agent list currently shows every loaded agent at full emphasis, including those that are not used in the selected project. That makes it hard to see which agents are actually relevant. As a first step (before filtering), unavailable agents should be visually de-emphasized so the relevant ones stand out.

## What Changes

- Dim agents that are not available in the currently selected project (reduced opacity)
- Keep dimmed agents selectable and usable (visual distinction only, not disabled)
- Show all agents at normal emphasis when no project is selected
- Recompute emphasis whenever the current project changes

## Capabilities

### New Capabilities

- `agent-availability-display`: visually distinguishing agents that are not available in the current project

### Modified Capabilities

<!-- None: agent-detection already computes availability; this only presents it. -->

## Impact

**Modified code:**
- `src/CLIHub/Windows/AgentItem.cs` — carry an availability flag and a derived opacity
- `src/CLIHub/Windows/MainWindow.xaml` — bind row opacity to the availability flag
- `src/CLIHub/Windows/MainWindow.xaml.cs` — set availability from `IAgentDetectionService` on refresh

**Reuses:**
- `IAgentDetectionService.IsAvailableInProject` (from `agent-detection`)

**No breaking changes.**
