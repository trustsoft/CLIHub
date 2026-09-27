## 1. Models

- [x] 1.1 Add `AgentCommandKind` enum (`Launch`, `Resume`, `Version`, `Update`, `Init`) in `src/CLIHub.Core/Models/AgentCommandKind.cs` and verify it compiles
- [x] 1.2 Add `AgentCommands` in `src/CLIHub.Core/Models/AgentCommands.cs` with nullable `Launch/Resume/Version/Update/Init` and `Get(AgentCommandKind)` and verify it compiles
- [x] 1.3 Add `AgentDetection` in `src/CLIHub.Core/Models/AgentDetection.cs` with `SystemPaths` and `ProjectIndicators` lists and verify it compiles
- [x] 1.4 Change `Plugin.Commands` to `AgentCommands` and add `Plugin.Detection` (default empty), and verify `CLIHub.Core` compiles

## 2. Process Launching

- [x] 2.1 Add `CaptureOutputAsync(executable, arguments, workingDirectory, ct)` to `IProcessLauncher` returning started/exit/stdout/stderr with redirected output, no window, and a timeout that kills the process, and verify `CLIHub.Core` compiles
- [x] 2.2 Implement `CaptureOutputAsync` in `ProcessLauncher` and verify a unit test captures output of a trivial command (for example `cmd /c echo`)

## 3. Command Execution

- [x] 3.1 Add `IAgentCommandService` + `AgentCommandResult(Success, Output, Error)` in `src/CLIHub.Core/Interfaces/IAgentCommandService.cs` and verify it compiles
- [x] 3.2 Implement `AgentCommandService.ExecuteAsync`: validate the plugin defines the kind and the project folder exists, route `Launch/Resume/Init/Update` to terminal launch and `Version` to `CaptureOutputAsync`, and return a uniform result, and verify `CLIHub.Core` compiles

## 4. Detection

- [x] 4.1 Add `IAgentDetectionService` + `AgentDetectionService` in `src/CLIHub.Core` with `IsInstalledInSystem` (expand env vars, file/dir exists) and `IsAvailableInProject` (combine project path + indicator, missing folder → false), and verify `CLIHub.Core` compiles

## 5. Plugin Loading

- [x] 5.1 Update `PluginManager` validation to require `Id`, `Name`, and `Commands.Launch`; default `Detection` when absent; log a warning naming the missing piece and verify a unit test covers accept/reject
- [x] 5.2 Register `IAgentCommandService`, `IAgentDetectionService`, and their implementations in `AddClIHubCoreServices` and verify the container resolves them

## 6. UI Integration

- [x] 6.1 Update `MainWindow` to list agents with system/project availability (via `IAgentDetectionService`) and add actions Launch, Resume, Init, Update, Get version (disabled when the command is undefined), and verify the window compiles and runs
- [x] 6.2 Update `TrayIconController` to list launchable agents for the current project and launch the selected agent via `IAgentCommandService`, and verify the menu builds
- [x] 6.3 Update the sample `plugin.json` under `%APPDATA%\CLIHub\plugins\opencode` to the new schema and verify it loads (task-level: also add a repo sample descriptor)

## 7. Tests

- [x] 7.1 Add `AgentCommandServiceTests` (routing: version captures, others launch; missing kind; missing project) using a fake `IProcessLauncher`
- [x] 7.2 Add `AgentDetectionServiceTests` (system marker present/absent, env expansion, project indicator present/absent, missing project)
- [x] 7.3 Add/adjust `PluginManager` validation tests for the new schema and verify `dotnet test` passes

## 8. Verification

- [x] 8.1 Build the full solution with 0 warnings/errors
- [x] 8.2 Manually verify: an agent with a `version` command reports its version; Launch opens a terminal in the project; Resume/Init do what the descriptor says
- [x] 8.3 Manually verify: availability reflects real markers (create/remove a project indicator and refresh)

> Verified: plugin loads with the new schema (`INF Loaded plugin opencode (OpenCode)`); 74/74 tests pass; build 0 warnings/errors. User-confirmed 8.2 (Launch/Resume work; Version reports `1.18.32` after the `%COMSPEC% /c` fix; opencode has no `init` so its descriptor omits it) and 8.3 (availability matches markers).
