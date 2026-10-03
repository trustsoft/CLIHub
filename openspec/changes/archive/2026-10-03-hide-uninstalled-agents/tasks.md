## 1. ViewModel filtering

- [x] 1.1 In `LaunchWindowViewModel.RefreshAgents`, skip plugins for which `IAgentDetectionService.IsInstalledInSystem` is false, before the project-availability computation; verify by building the solution (`dotnet build CLIHub.sln`)
- [x] 1.2 Add unit tests covering the new list behavior (uninstalled agent excluded; installed agent still listed and subject to project dimming/filter; installed-only check runs before the project filter regardless of the preference; empty list when nothing is installed), mocking `IAgentDetectionService`; verify with `dotnet test tests/CLIHub.Tests`

## 2. Verification

- [x] 2.1 Manually verify in the running app: a plugin whose system paths do not exist (for example a temporarily renamed marker folder for Qwen Code) disappears from the launch window, reappears after Restore/Refresh; verify the hint text/empty state reads sensibly when the list becomes empty
- [x] 2.2 Run `openspec validate "hide-uninstalled-agents"` and confirm it passes before archiving
