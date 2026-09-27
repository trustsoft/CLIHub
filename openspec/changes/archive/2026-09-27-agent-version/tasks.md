## 1. Version Service

- [x] 1.1 Add `IAgentVersionService` in `src/CLIHub.Core/Interfaces/IAgentVersionService.cs` with `Task<string?> GetVersionAsync(Plugin, CancellationToken)` and `void Invalidate()`, and verify it compiles
- [x] 1.2 Implement `AgentVersionService` in `src/CLIHub.Core/Services/` reusing `IProcessLauncher.CaptureOutputAsync`, running the version command in the user profile directory, extracting the version number from the output (falling back to the first non-empty line), returning null when the command is missing or fails (logged), and caching per plugin id with `Invalidate()`, and verify `CLIHub.Core` compiles
- [x] 1.3 Register `IAgentVersionService` in `AddClIHubCoreServices` and verify the container resolves it

## 2. UI

- [x] 2.1 Make `AgentItem` carry a mutable `Version` with `INotifyPropertyChanged` (and keep `Status`/`Name`/`LogoPath`), and verify it compiles
- [x] 2.2 In `MainWindow.RefreshAgents`, render the items immediately (version shown as a placeholder), then populate versions asynchronously via `Task.WhenAll`, updating each item; invalidate the cache on explicit refresh, and verify the window compiles and runs
- [x] 2.3 Show the version in each agent row (for example `Version: 1.18.32` or `unknown`), and verify it renders

## 3. Tests

- [x] 3.1 Add `AgentVersionServiceTests`: returns output when the command exists; extracts the version number from verbose output; returns null and does not spawn when no version command; returns null on failure; caches across calls; `Invalidate` forces a re-run
- [x] 3.2 Verify `dotnet test` passes

## 4. Verification

- [x] 4.1 Build the full solution with 0 warnings/errors
- [x] 4.2 Manually verify: the agent list shows each agent's version (for example OpenCode 1.18.x) and `unknown` where unavailable, without freezing the window

> Verified: agent list shows versions for all six agents (OpenCode 1.18.32, Pi 0.85.1, Cline 3.0.65, GitHub Copilot 1.0.88, OpenClaude 0.30.0, Qwen 0.24.1) without freezing; the version number is extracted from verbose output. 91/91 tests pass; build 0 warnings/errors. User-confirmed 4.2.
