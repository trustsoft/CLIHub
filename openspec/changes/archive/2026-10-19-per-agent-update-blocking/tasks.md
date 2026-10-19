# Tasks: per-agent-update-blocking

## 1. Monitor

- [ ] 1.1 Add `HasRunningAgents(Plugin)` and `GetRunningAgents(Plugin)` filtering by agent id

## 2. Consumers

- [ ] 2.1 `WindowActionCoordinator`: per-agent availability and execution-time double check
- [ ] 2.2 `AgentManagementService.UpdateAsync`: per-agent block with updated message

## 3. Tests

- [ ] 3.1 Monitor: per-agent filtering returns only the requested agent's processes
- [ ] 3.2 Service: update starts when only other agents run; blocks when the same agent runs

## 4. Verification

- [ ] 4.1 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
- [ ] 4.2 Live: Update enabled for non-running agents, still disabled for OpenCode while its processes run
