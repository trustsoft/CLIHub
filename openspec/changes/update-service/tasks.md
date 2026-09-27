## 1. Service

- [ ] 1.1 Add the `Velopack` package to `src/CLIHub.Core/CLIHub.Core.csproj` and verify restore succeeds
- [ ] 1.2 Add `src/CLIHub.Core/Interfaces/IUpdateService.cs` with `UpdateStatus`, `UpdateCheckResult`, `GetCurrentVersion()`, and `CheckForUpdatesAsync(CancellationToken)` and verify it compiles
- [ ] 1.3 Implement `UpdateService` in `src/CLIHub.Core/Services/`: build `UpdateManager` from a repository-URL constant; return `NotInstalled` when `CurrentVersion` is null; `UpToDate`/`UpdateAvailable(version)` from `CheckForUpdatesAsync`; 15s timeout; catch and log failures, and verify `CLIHub.Core` compiles
- [ ] 1.4 Register `IUpdateService` in `AddClIHubCoreServices` and verify the container resolves it

## 2. UI

- [ ] 2.1 Add `NotifyUpdateAvailable(string version)` to `TrayIconController` (tray notification via H.NotifyIcon) and verify it compiles
- [ ] 2.2 In `App.OnStartup`, run `CheckForUpdatesAsync` fire-and-forget and, on `UpdateAvailable`, call the tray notification; log the outcome, and verify the app starts
- [ ] 2.3 In `MainWindow`, display the current app version and add a "Check for updates" button that runs the check and reports the result in the status bar, and verify it renders

## 3. Tests

- [ ] 3.1 Add `UpdateServiceTests`: `GetCurrentVersion` returns a non-empty string; `CheckForUpdatesAsync` returns `NotInstalled` (no network) when `CurrentVersion` is null; a failing check returns `Failed` without throwing
- [ ] 3.2 Verify `dotnet test` passes

## 4. Verification

- [ ] 4.1 Build the full solution with 0 warnings/errors
- [ ] 4.2 Manually verify: the window shows the app version; clicking "Check for updates" reports an outcome (in a build-output run, "not installed"); startup does not block
