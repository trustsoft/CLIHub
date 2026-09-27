## Why

The agent list shows availability but not what is actually installed. Seeing each agent's version at a glance helps choose between agents and spot outdated installs. The version command already exists in the descriptor (`agent-commands`), so the app can capture it per agent.

## What Changes

- Retrieve each agent's version by running its `version` command and capturing output, independent of any selected project
- Cache versions per agent so the list does not re-run processes on every refresh
- Render the agent list immediately, then fill in versions asynchronously (no UI blocking)
- Show `unknown` for agents without a version command or when the command fails
- Provide a way to invalidate the cache (refresh)

## Capabilities

### New Capabilities

- `agent-version`: project-independent version lookup, caching, failure handling, and presentation in the agent list

### Modified Capabilities

<!-- None: the project-scoped version execution in agent-commands is unchanged. -->

## Impact

**New code:**
- `src/CLIHub.Core/Interfaces/IAgentVersionService.cs`, `src/CLIHub.Core/Services/AgentVersionService.cs`

**Modified code:**
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — register `IAgentVersionService`
- `src/CLIHub/Windows/MainWindow.xaml(.cs)` — show and asynchronously populate per-agent versions
- `src/CLIHub/Windows/AgentItem.cs` — carry a mutable version with change notification

**Reuses:**
- `IProcessLauncher.CaptureOutputAsync` (from `agent-commands`)
- `Plugin.Commands.Version` and `AgentDetection` (from `agent-commands`)

**No breaking changes.**
