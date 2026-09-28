## 1. Cache invalidation

- [x] 1.1 Add `void Invalidate()` to `IAgentDetectionService` and implement it in `AgentDetectionService` (clear the cache); add a test verifying a result cached within TTL is re-checked after `Invalidate()`. Verify with `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`.
- [x] 1.2 In `src/CLIHub/Windows/MainWindow.xaml.cs`, call `IAgentDetectionService.Invalidate()` from `Refresh_Click` (alongside the version invalidation) and update the status text. Verify with `dotnet build CLIHub.sln`.

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then verify manually: add a project indicator for an agent and click **Refresh** — availability updates without waiting for the TTL.
