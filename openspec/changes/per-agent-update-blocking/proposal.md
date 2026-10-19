# Block an agent update only while that same agent is running

## Why

The launch-window Actions menu and the Settings agent cards disable every agent's Update action
whenever any registered agent process is running. On a machine where an agent session is active
most of the day (for example an OpenCode TUI plus its background service), no agent can ever be
updated.

Both behavior specs already describe the narrower rule — "WHEN **a matching agent process** is
running" — but the implementation checks `HasRunningAgents()` globally. Updating agent X while
agent Y runs is safe; updating X while X itself runs is not.

## What Changes

- `AgentProcessMonitor` gains per-agent overloads: `HasRunningAgents(Plugin)` and
  `GetRunningAgents(Plugin)`, filtering matches by the agent's id.
- The launch-window Update availability (`WindowActionCoordinator`) and the execution-time
  double check consider only processes of the agent being updated.
- The Settings-path update (`AgentManagementService.UpdateAsync`) applies the same per-agent
  check and reports "Update blocked while the agent is running".
- Tests: monitor per-agent filtering; service starts the update when only other agents run and
  blocks when the same agent runs.

## Impact

- Affected specs: `agent-ui` (update blocked by a running agent), `preferences-ui` (update
  blocked while running) — clarified to name the same-agent rule explicitly
- Affected code:
  - `src/CLIHub.Core/Agents/AgentProcessMonitor.cs`
  - `src/CLIHub.Core/Agents/AgentManagementService.cs`
  - `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs`
  - `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs`
  - `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`
