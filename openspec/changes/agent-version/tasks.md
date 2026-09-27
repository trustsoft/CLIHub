## 1. Version Service

- [ ] 1.1 Add `IAgentVersionService` in `src/CLIHub.Core/Interfaces/IAgentVersionService.cs` with `Task<string?> GetVersionAsync(Plugin, CancellationToken)` and `void Invalidate()`, and verify it compiles
- [ ] 1.2 Implement `AgentVersionService` in `src/CLIHub.Core/Services/` reusing `IProcessLauncher.CaptureOutputAsync`, running the version command in the user profile directory, returning the first non-empty output line, returning null when the command is missing or fails (logged), and caching per plugin id with `Invalidate()`, and verify `CLIHub.Core` compiles
- [ ] 1.3 Register `IAgentVersionService` in `AddClIHubCoreServices` and verify the container resolves it

## 2. UI

- [ ] 2.1 Make `AgentItem` carry a mutable `Version` with `INotifyPropertyChanged` (and keep `Status`/`Name`/`LogoPath`), and verify it compiles
- [ ] 2.2 In `MainWindow.RefreshAgents`, render the items immediately (version shown as `unknown`), then populate versions asynchronously via `Task.WhenAll`, updating each item; invalidate the cache on explicit refresh, and verify the window compiles and runs
- [ ] 2.3 Show the version in each agent row (for example `v1.18.32` or `unknown`), and verify it renders

## 3. Tests

- [ ] 3.1 Add `AgentVersionServiceTests`: returns output when the command exists; returns null and does not spawn when no version command; returns null on failure; caches across calls; `Invalidate` forces a re-run
- [ ] 3.2 Verify `dotnet test` passes

## 4. Verification

- [ ] 4.1 Build the full solution with 0 warnings/errors
- [ ] 4.2 Manually verify: the agent list shows each agent's version (for example OpenCode 1.18.x) and `unknown` where unavailable, without freezing the window
