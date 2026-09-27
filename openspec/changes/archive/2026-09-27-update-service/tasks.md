## 1. Service

- [x] 1.1 Add the `Velopack` package to `src/CLIHub.Core/CLIHub.Core.csproj` and verify restore succeeds
- [x] 1.2 Add `src/CLIHub.Core/Interfaces/IUpdateService.cs` with `UpdateStatus`, `UpdateCheckResult`, `GetCurrentVersion()`, and `CheckForUpdatesAsync(CancellationToken)` and verify it compiles
- [x] 1.3 Implement `UpdateService` in `src/CLIHub.Core/Services/`: build `UpdateManager` from a repository-URL constant; return `NotInstalled` when `CurrentVersion` is null; `UpToDate`/`UpdateAvailable(version)` from `CheckForUpdatesAsync`; 15s timeout; catch and log failures, and verify `CLIHub.Core` compiles
- [x] 1.4 Register `IUpdateService` in `AddClIHubCoreServices` and verify the container resolves it

## 2. UI

- [x] 2.1 Add `NotifyUpdateAvailable(string version)` to `TrayIconController` (tray notification via H.NotifyIcon) and verify it compiles
- [x] 2.2 In `App.OnStartup`, run `CheckForUpdatesAsync` fire-and-forget and, on `UpdateAvailable`, call the tray notification; log the outcome, and verify the app starts
- [x] 2.3 In `MainWindow`, display the current app version and add a "Check for updates" button that runs the check and reports the result in the status bar, and verify it renders

## 3. Tests

- [x] 3.1 Add `UpdateServiceTests`: `GetCurrentVersion` returns a non-empty string; `CheckForUpdatesAsync` returns `NotInstalled` (no network) when `CurrentVersion` is null; a failing check returns `Failed` without throwing
- [x] 3.2 Verify `dotnet test` passes

## 4. Verification

- [x] 4.1 Build the full solution with 0 warnings/errors
- [x] 4.2 Manually verify: the window shows the app version; clicking "Check for updates" reports an outcome (in a build-output run, "not installed"); startup does not block

> Implementation notes:
> - Velopack requires `VelopackApp.Build().Run()` before any `UpdateManager` is created, so a custom entry point (`Program.cs`, `<StartupObject>CLIHub.Program</StartupObject>`) was added; `Main` runs the bootstrap first, per the Velopack Run contract.
> - The source is `GithubSource` (GitHub Releases), per the docs.
> - `UpdateService` has an internal `UpdateManager` seam so tests exercise the installed path via `TestVelopackLocator` + `SimpleFileSource`; `CLIHub.Core` exposes internals to `CLIHub.Tests`.
> - The displayed version is normalized to a plain semantic version (`1.0.0`, no `+hash`).
> Verified: app runs; log shows `Update check skipped: the application is not a Velopack install`; 95/95 tests pass; build 0 warnings/errors. 4.2 awaits confirmation.
